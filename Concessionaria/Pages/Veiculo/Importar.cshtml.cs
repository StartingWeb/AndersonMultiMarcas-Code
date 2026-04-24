using Core.Enums;
using Core.Interfaces;
using Data;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Concessionaria.Pages.Veiculo;

public class ImportarModel : PageModel
{
    private static readonly string[] LabelOrder =
    [
        "Tipo de Cadastro",
        "Status",
        "Loja",
        "Marca",
        "Modelo",
        "KM",
        "Portas",
        "Cor",
        "Combustível",
        "Combustivel",
        "Câmbio",
        "Cambio",
        "Descrição",
        "Descricao",
        "Opcionais",
        "Valor",
        "Ano",
        "Ano Exibição",
        "Ano Exibicao",
        "Placa (não é exibido no anúncio)",
        "Placa (nao e exibido no anuncio)",
        "Placa",
        "Cilindradas",
        "Tipo de Moto",
        "Descrição Interna",
        "Descricao Interna",
        "Data Venda"
    ];

    private readonly ApplicationDbContext _context;
    private readonly IVeiculoService _veiculoService;
    private readonly IVeiculoCaracteristicaService _veiculoCaracteristicaService;

    public ImportarModel(
        ApplicationDbContext context,
        IVeiculoService veiculoService,
        IVeiculoCaracteristicaService veiculoCaracteristicaService)
    {
        _context = context;
        _veiculoService = veiculoService;
        _veiculoCaracteristicaService = veiculoCaracteristicaService;
    }

    [BindProperty]
    public ImportInputModel Input { get; set; } = new();

    public ImportPreviewModel? Preview { get; private set; }

    public async Task OnGetAsync()
    {
        await Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.RawContent))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.RawContent)}", "Cole o conteúdo do site antigo para gerar a prévia.");
            return Page();
        }

        Preview = await BuildPreviewAsync(Input.RawContent);
        return Page();
    }

    public async Task<IActionResult> OnPostImportAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.RawContent))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.RawContent)}", "Cole o conteúdo do site antigo para importar.");
            return Page();
        }

        Preview = await BuildPreviewAsync(Input.RawContent);
        if (!Preview.CanImport)
        {
            foreach (var erro in Preview.Errors)
            {
                ModelState.AddModelError(string.Empty, erro);
            }

            return Page();
        }

        var veiculo = Preview.ToVeiculo();
        var createResult = await _veiculoService.CriarAsync(veiculo);
        if (createResult.Status != PackageStatus.Success)
        {
            ModelState.AddModelError(string.Empty, createResult.UserMessage ?? "Não foi possível importar o veículo.");
            return Page();
        }

        var caracteristica = Preview.ToCaracteristica(createResult.Data);
        var caracteristicaResult = await _veiculoCaracteristicaService.CriarOuAtualizarAsync(caracteristica);
        if (caracteristicaResult.Status != PackageStatus.Success)
        {
            ModelState.AddModelError(string.Empty, caracteristicaResult.UserMessage ?? "O veículo foi criado, mas não foi possível salvar os opcionais.");
            return Page();
        }

        TempData["SuccessMessage"] = $"Veículo importado com sucesso. ID criado: {createResult.Data}.";
        return RedirectToPage("./Upsert", new { id = createResult.Data });
    }

    private async Task<ImportPreviewModel> BuildPreviewAsync(string rawContent)
    {
        var fields = ParseRawContent(rawContent);
        var preview = new ImportPreviewModel
        {
            RawFields = fields
        };

        preview.TipoCadastro = GetValue(fields, "Tipo de Cadastro");
        preview.Status = GetValue(fields, "Status");
        preview.LojaNome = GetValue(fields, "Loja");
        preview.MarcaNome = GetValue(fields, "Marca");
        preview.ModeloNome = GetValue(fields, "Modelo");
        preview.KmTexto = GetValue(fields, "KM");
        preview.Portas = GetValue(fields, "Portas");
        preview.Cor = GetValue(fields, "Cor");
        preview.Combustivel = GetValue(fields, "Combustível", "Combustivel");
        preview.Cambio = GetValue(fields, "Câmbio", "Cambio");
        preview.Descricao = CleanLongText(GetValue(fields, "Descrição", "Descricao"));
        preview.Opcionais = CleanLongText(GetValue(fields, "Opcionais"));
        preview.ValorTexto = GetValue(fields, "Valor");
        preview.AnoTexto = GetValue(fields, "Ano");
        preview.AnoExibicaoTexto = GetValue(fields, "Ano Exibição", "Ano Exibicao");
        preview.Placa = GetValue(fields, "Placa (não é exibido no anúncio)", "Placa (nao e exibido no anuncio)", "Placa");
        preview.Cilindradas = GetValue(fields, "Cilindradas");
        preview.TipoMoto = GetValue(fields, "Tipo de Moto");
        preview.DescricaoInterna = CleanLongText(GetValue(fields, "Descrição Interna", "Descricao Interna"));
        preview.DataVendaTexto = GetValue(fields, "Data Venda");

        preview.Loja = await ResolveLojaAsync(preview.LojaNome);
        if (preview.Loja == null)
        {
            preview.Errors.Add($"Loja não encontrada: {preview.LojaNome ?? "(vazia)"}.");
        }

        preview.Marca = await ResolveOrCreateMarcaAsync(preview.MarcaNome, preview.Errors);
        preview.Quilometragem = ParseNullableInt(preview.KmTexto);
        preview.PrecoVenda = ParseNullableDecimal(preview.ValorTexto);
        preview.AnoFabricacao = ParseNullableInt(preview.AnoTexto);
        preview.AnoModelo = ParseNullableInt(preview.AnoExibicaoTexto) ?? preview.AnoFabricacao;
        preview.DataVenda = ParseNullableDate(preview.DataVendaTexto);

        if (string.IsNullOrWhiteSpace(preview.ModeloNome))
        {
            preview.Errors.Add("Modelo não informado no conteúdo importado.");
        }

        if (preview.Marca == null)
        {
            preview.Errors.Add("Marca não pôde ser resolvida.");
        }

        preview.Seminuovo = ContainsValue(preview.Status, "seminovo");
        preview.MotoEletrica = ContainsValue(preview.Status, "moto eletrica")
            || ContainsValue(preview.Status, "moto elétrica");
        preview.Vendido = preview.DataVenda.HasValue;
        preview.Titulo = BuildTitulo(preview.MarcaNome, preview.ModeloNome);
        preview.ObservacoesInternas = BuildObservacoesInternas(preview);
        preview.Carateristica = ParseOpcionais(preview.Opcionais, preview.Cambio, preview.Combustivel);
        preview.CanImport = preview.Errors.Count == 0;

        return preview;
    }

    private static Dictionary<string, string> ParseRawContent(string rawContent)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = rawContent
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => !ShouldIgnoreLine(line))
            .ToList();

        int index = 0;
        while (index < lines.Count)
        {
            var label = MatchLabel(lines[index]);
            if (label == null)
            {
                index++;
                continue;
            }

            index++;
            var buffer = new List<string>();

            while (index < lines.Count && MatchLabel(lines[index]) == null)
            {
                if (!ShouldIgnoreLine(lines[index]))
                {
                    buffer.Add(lines[index]);
                }

                index++;
            }

            fields[label] = string.Join("\n", buffer).Trim();
        }

        return fields;
    }

    private static bool ShouldIgnoreLine(string line)
    {
        var normalized = Normalize(line);
        return normalized.Contains("| editar") ||
               normalized == "produtos | editar" ||
               normalized.StartsWith("gerenciar ") ||
               normalized == "funcoes" ||
               normalized == "anderson multimarcas | gerenciador de conteudo ®" ||
               normalized == "anderson multimarcas | gerenciador de conteudo" ||
               normalized.StartsWith("desenvolvido por:") ||
               normalized == "formatacao" ||
               normalized == "tamanho" ||
               normalized.StartsWith("endereco:words:");
    }

    private static string? MatchLabel(string line)
    {
        var normalized = Normalize(line);
        return LabelOrder.FirstOrDefault(label => Normalize(label) == normalized);
    }

    private static string? GetValue(Dictionary<string, string> fields, params string[] labels)
    {
        foreach (var label in labels)
        {
            if (fields.TryGetValue(label, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private async Task<Domain.Loja?> ResolveLojaAsync(string? lojaNome)
    {
        if (string.IsNullOrWhiteSpace(lojaNome))
        {
            return null;
        }

        var normalized = Normalize(lojaNome);
        var lojas = await _context.Lojas
            .OrderBy(loja => loja.Id)
            .ToListAsync();

        var exata = lojas.FirstOrDefault(loja => Normalize(loja.Nome) == normalized);
        if (exata != null)
        {
            return exata;
        }

        var contem = lojas.FirstOrDefault(loja =>
            Normalize(loja.Nome).Contains(normalized) ||
            normalized.Contains(Normalize(loja.Nome)));

        if (contem != null)
        {
            return contem;
        }

        if (normalized.StartsWith("loja "))
        {
            var digits = new string(normalized.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var index) && index > 0)
            {
                var ordered = lojas.OrderBy(loja => loja.Id).ToList();
                if (index <= ordered.Count)
                {
                    return ordered[index - 1];
                }
            }
        }

        if (lojas.Count == 1)
        {
            return lojas[0];
        }

        return null;
    }

    private async Task<Domain.Marca?> ResolveOrCreateMarcaAsync(string? marcaNome, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(marcaNome))
        {
            return null;
        }

        var normalized = Normalize(marcaNome);
        var marcas = await _context.Marcas.ToListAsync();
        var marca = marcas.FirstOrDefault(item => Normalize(item.Nome) == normalized);

        if (marca != null)
        {
            return marca;
        }

        try
        {
            marca = new Domain.Marca
            {
                Nome = marcaNome.Trim(),
                Ativo = true,
                DataCadastro = DateTime.Now
            };

            _context.Marcas.Add(marca);
            await _context.SaveChangesAsync();
            return marca;
        }
        catch (Exception ex)
        {
            errors.Add($"Não foi possível criar a marca '{marcaNome}': {ex.Message}");
            return null;
        }
    }

    private static string BuildTitulo(string? marca, string? modelo)
    {
        var parts = new[] { marca, modelo }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());

        return string.Join(" ", parts).Trim();
    }

    private static string? BuildObservacoesInternas(ImportPreviewModel preview)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(preview.DescricaoInterna))
        {
            parts.Add($"Descrição interna: {preview.DescricaoInterna}");
        }

        if (!string.IsNullOrWhiteSpace(preview.TipoCadastro))
        {
            parts.Add($"Tipo de cadastro antigo: {preview.TipoCadastro}");
        }

        if (!string.IsNullOrWhiteSpace(preview.Portas))
        {
            parts.Add($"Portas: {preview.Portas}");
        }

        if (!string.IsNullOrWhiteSpace(preview.Cilindradas))
        {
            parts.Add($"Cilindradas: {preview.Cilindradas}");
        }

        if (!string.IsNullOrWhiteSpace(preview.TipoMoto))
        {
            parts.Add($"Tipo de moto: {preview.TipoMoto}");
        }

        return parts.Count == 0 ? null : string.Join(" | ", parts);
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
        result.VidroEletrico = HasOption(normalized, "vidro eletrico");
        result.TravaEletrica = HasOption(normalized, "trava eletrica");
        result.RetrovisorEletrico = HasOption(normalized, "retrovisor eletrico");
        result.BancoDeCouro = HasOption(normalized, "banco de couro");
        result.VolanteMultifuncional = HasOption(normalized, "volante multifuncional");
        result.PilotoAutomatico = HasOption(normalized, "piloto automatico", "controle de cruzeiro");
        result.ComputadorBordo = HasOption(normalized, "computador de bordo");
        result.ChavePresencial = HasOption(normalized, "chave presencial");
        result.PartidaBotao = HasOption(normalized, "partida por botao");
        result.AirbagMotorista = HasOption(normalized, "airbag motorista", "airbag duplo", "air bag motorista");
        result.AirbagPassageiro = HasOption(normalized, "airbag passageiro", "airbag duplo", "air bag passageiro");
        result.Alarme = HasOption(normalized, "alarme");
        result.FreiosAbs = HasOption(normalized, "abs", "freios abs");
        result.CameraDeRe = HasOption(normalized, "camera de re");
        result.SensorEstacionamentoTraseiro = HasOption(normalized, "sensor de estacionamento", "sensor traseiro");
        result.SensorEstacionamentoDianteiro = HasOption(normalized, "sensor dianteiro");
        result.CentralMultimidia = HasOption(normalized, "central multimidia", "multimidia");
        result.Som = HasOption(normalized, "som");
        result.Bluetooth = HasOption(normalized, "bluetooth");
        result.Usb = HasOption(normalized, "usb");
        result.Radio = HasOption(normalized, "radio");
        result.GPS = HasOption(normalized, "gps");
        result.AppleCarPlay = HasOption(normalized, "carplay", "apple carplay");
        result.AndroidAuto = HasOption(normalized, "android auto");
        result.RodaLigaLeve = HasOption(normalized, "roda de liga", "rodas de liga");
        result.CapotaMaritima = HasOption(normalized, "capota maritima");
        result.Estribo = HasOption(normalized, "estribo");
        result.SantoAntonio = HasOption(normalized, "santo antonio");
        result.ProtetorCacamba = HasOption(normalized, "protetor de cacamba");
        result.CambioManual = normalizedCambio.Contains("manual");
        result.CambioAutomatico = normalizedCambio.Contains("automatic");
        result.CambioCvt = normalizedCambio.Contains("cvt");
        result.Turbo = HasOption(normalized, "turbo");
        result.TracaoIntegral = HasOption(normalized, "4x4", "tracao integral");
        result.TracaoDianteira = HasOption(normalized, "tracao dianteira");
        result.TracaoTraseira = HasOption(normalized, "tracao traseira");
        result.Hibrido = normalizedCombustivel.Contains("hibrido");
        result.Eletrico = normalizedCombustivel.Contains("eletric");

        return result;
    }

    private static bool HasOption(string source, params string[] values)
    {
        return values.Any(value => source.Contains(Normalize(value)));
    }

    private static bool ContainsValue(string? source, string value)
    {
        return Normalize(source ?? string.Empty).Contains(Normalize(value));
    }

    private static int? ParseNullableInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var parsed) ? parsed : null;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = new string(value.Trim()
            .Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',')
            .ToArray());

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return null;
        }

        var lastDot = sanitized.LastIndexOf('.');
        var lastComma = sanitized.LastIndexOf(',');
        char? decimalSeparator = null;

        if (lastDot >= 0 && lastComma >= 0)
        {
            decimalSeparator = lastDot > lastComma ? '.' : ',';
        }
        else if (lastDot >= 0 || lastComma >= 0)
        {
            var separator = lastDot >= 0 ? '.' : ',';
            var lastIndex = sanitized.LastIndexOf(separator);
            var trailingDigits = sanitized.Length - lastIndex - 1;

            if (trailingDigits is 1 or 2)
            {
                decimalSeparator = separator;
            }
        }

        var digits = new string(sanitized.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
        {
            return null;
        }

        string normalized;
        if (decimalSeparator.HasValue)
        {
            var decimalIndex = sanitized.LastIndexOf(decimalSeparator.Value);
            var decimalDigits = sanitized.Length - decimalIndex - 1;

            if (decimalDigits <= 0)
            {
                normalized = digits;
            }
            else if (digits.Length <= decimalDigits)
            {
                normalized = $"0.{digits.PadLeft(decimalDigits, '0')}";
            }
            else
            {
                normalized = $"{digits[..^decimalDigits]}.{digits[^decimalDigits..]}";
            }
        }
        else
        {
            normalized = digits;
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? decimal.Round(parsed, 2, MidpointRounding.AwayFromZero)
            : null;
    }

    private static DateTime? ParseNullableDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd/MM/yyyy HH:mm", "d/M/yyyy H:m" };
        return DateTime.TryParseExact(value.Trim(), formats, new CultureInfo("pt-BR"), DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    private static string? CleanLongText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var lines = value
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
    }

    private static string Normalize(string value)
    {
        var formD = value.Trim().Normalize(NormalizationForm.FormD);
        var chars = formD.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).ToLowerInvariant();
    }

    public sealed class ImportInputModel
    {
        public string RawContent { get; set; } = string.Empty;
    }

    public sealed class ImportPreviewModel
    {
        public Dictionary<string, string> RawFields { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Errors { get; init; } = [];
        public bool CanImport { get; set; }
        public string? TipoCadastro { get; set; }
        public string? Status { get; set; }
        public string? LojaNome { get; set; }
        public Domain.Loja? Loja { get; set; }
        public string? MarcaNome { get; set; }
        public Domain.Marca? Marca { get; set; }
        public string? ModeloNome { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? KmTexto { get; set; }
        public int? Quilometragem { get; set; }
        public string? Portas { get; set; }
        public string? Cor { get; set; }
        public string? Combustivel { get; set; }
        public string? Cambio { get; set; }
        public string? Descricao { get; set; }
        public string? Opcionais { get; set; }
        public string? ValorTexto { get; set; }
        public decimal? PrecoVenda { get; set; }
        public string? AnoTexto { get; set; }
        public int? AnoFabricacao { get; set; }
        public string? AnoExibicaoTexto { get; set; }
        public int? AnoModelo { get; set; }
        public string? Placa { get; set; }
        public string? Cilindradas { get; set; }
        public string? TipoMoto { get; set; }
        public string? DescricaoInterna { get; set; }
        public string? DataVendaTexto { get; set; }
        public DateTime? DataVenda { get; set; }
        public bool Seminuovo { get; set; }
        public bool MotoEletrica { get; set; }
        public bool Vendido { get; set; }
        public string? ObservacoesInternas { get; set; }
        public VeiculoCaracteristica Carateristica { get; set; } = new();

        public Domain.Veiculo ToVeiculo()
        {
            return new Domain.Veiculo
            {
                LojaId = Loja!.Id,
                MarcaId = Marca!.Id,
                Titulo = string.IsNullOrWhiteSpace(Titulo) ? ModeloNome ?? "Importado do site antigo" : Titulo,
                Modelo = ModeloNome,
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
                Ativo = !Vendido
            };
        }

        public VeiculoCaracteristica ToCaracteristica(int veiculoId)
        {
            Carateristica.VeiculoId = veiculoId;
            return Carateristica;
        }
    }
}
