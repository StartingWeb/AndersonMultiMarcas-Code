using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Domain;
using Project.Services;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Project.Pages.Admin;

public class ConferenciaEstoqueModel : PageModel
{
    private static readonly ConcurrentDictionary<string, DownloadPayload> DownloadStore = new();
    private static readonly ConcurrentDictionary<string, CadastroJobStatus> CadastroJobs = new();
    private readonly ApplicationDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEstoqueConferenciaExcelService _conferenciaService;

    public ConferenciaEstoqueModel(
        ApplicationDbContext db,
        IServiceScopeFactory scopeFactory,
        IEstoqueConferenciaExcelService conferenciaService)
    {
        _db = db;
        _scopeFactory = scopeFactory;
        _conferenciaService = conferenciaService;
    }

    [BindProperty]
    public IFormFile? Arquivo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CadastroJobId { get; set; }

    public string? ErrorMessage { get; private set; }
    public string? DownloadToken { get; private set; }
    public string? DownloadFileName { get; private set; }
    public int TotalCarrosCadastrados { get; private set; }
    public int TotalCarrosNaoEncontrados { get; private set; }
    public int TotalCarrosDiferentes { get; private set; }
    public bool ResultadoDisponivel => !string.IsNullOrWhiteSpace(DownloadToken);
    public bool ExibirProgressoCadastro => !string.IsNullOrWhiteSpace(CadastroJobId) && CadastroJobs.ContainsKey(CadastroJobId);

    public void OnGet()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            return;
        }

        if (DownloadStore.TryGetValue(Token, out var payload))
        {
            FillFromPayload(Token, payload);
        }
    }

    public IActionResult OnGetCadastroProgresso(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId) || !CadastroJobs.TryGetValue(jobId, out var status))
        {
            return new JsonResult(new { found = false });
        }

        var percentual = status.Total > 0
            ? (int)Math.Round((status.Processados / (double)status.Total) * 100d)
            : 0;

        return new JsonResult(new
        {
            found = true,
            running = status.Running,
            completed = status.Completed,
            total = status.Total,
            processados = status.Processados,
            cadastrados = status.Cadastrados,
            ignorados = status.Ignorados,
            percentual,
            mensagem = status.Message
        });
    }

    public IActionResult OnGetDownload(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !DownloadStore.TryGetValue(token, out var payload))
        {
            return NotFound();
        }

        return File(
            payload.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            payload.FileName);
    }

    public IActionResult OnGetDownloadNaoEncontrados(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !DownloadStore.TryGetValue(token, out var payload))
        {
            return NotFound();
        }

        return File(
            payload.NotFoundContent,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            payload.NotFoundFileName);
    }

    public async Task<IActionResult> OnPostProcessarAsync(CancellationToken cancellationToken)
    {
        if (Arquivo == null || Arquivo.Length == 0)
        {
            ErrorMessage = "Selecione um arquivo Excel para processar.";
            return Page();
        }

        var extension = Path.GetExtension(Arquivo.FileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Formato invalido. Envie um arquivo .xlsx.";
            return Page();
        }

        await using var stream = Arquivo.OpenReadStream();
        var output = await _conferenciaService.ProcessarAsync(stream, cancellationToken);

        var token = Guid.NewGuid().ToString("N");
        DownloadStore[token] = new DownloadPayload(
            output.Content,
            output.FileName,
            output.NotFoundContent,
            output.NotFoundFileName,
            output.TotalCadastrados,
            output.TotalNaoEncontrados,
            output.TotalDivergencias,
            output.CadastrosNaoEncontrados,
            output.CorrecoesPorPlaca);

        return RedirectToPage(new { token });
    }

    public IActionResult OnPostIniciarCadastroNaoEncontradosAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !DownloadStore.TryGetValue(token, out var payload))
        {
            TempData["Error"] = "Resultado da conferencia nao encontrado. Processe novamente.";
            return RedirectToPage();
        }

        if (payload.CadastrosNaoEncontrados.Count == 0)
        {
            TempData["Success"] = "Nao ha itens nao encontrados para cadastrar.";
            return RedirectToPage(new { token });
        }

        var jobId = Guid.NewGuid().ToString("N");
        CadastroJobs[jobId] = new CadastroJobStatus
        {
            Running = true,
            Completed = false,
            Total = payload.CadastrosNaoEncontrados.Count,
            Processados = 0,
            Cadastrados = 0,
            Ignorados = 0,
            Message = "Iniciando cadastro..."
        };

        var itens = payload.CadastrosNaoEncontrados.ToList();
        _ = Task.Run(() => ProcessarCadastroNaoEncontradosJobAsync(jobId, itens));

        TempData["Success"] = "Cadastro iniciado. Acompanhe pela barra de progresso.";
        return RedirectToPage(new { token, cadastroJobId = jobId });
    }

    private async Task ProcessarCadastroNaoEncontradosJobAsync(
        string jobId,
        IReadOnlyList<ConferenciaNaoEncontradoCadastroItem> itens)
    {
        if (!CadastroJobs.TryGetValue(jobId, out var status))
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var lojaId = await db.Lojas
                .AsNoTracking()
                .OrderByDescending(item => item.Ativo)
                .ThenBy(item => item.Id)
                .Select(item => (int?)item.Id)
                .FirstOrDefaultAsync();

            if (!lojaId.HasValue)
            {
                status.Message = "Nao existe loja cadastrada para vincular os novos veiculos.";
                return;
            }

            var marcas = await db.Marcas.ToListAsync();
            var semMarca = marcas.FirstOrDefault(item => string.Equals(item.Nome, "Sem Marca", StringComparison.OrdinalIgnoreCase));
            if (semMarca == null)
            {
                semMarca = new Marca
                {
                    Nome = "Sem Marca",
                    LogoUrl = string.Empty,
                    Ativo = true,
                    DataCadastro = DateTime.Now
                };
                db.Marcas.Add(semMarca);
                await db.SaveChangesAsync();
                marcas.Add(semMarca);
            }

            var existingVehicles = await db.Veiculos
                .AsNoTracking()
                .Select(item => new
                {
                    item.Placa,
                    item.Titulo,
                    Ano = item.AnoModelo ?? item.AnoFabricacao
                })
                .ToListAsync();

            var existingPlateKeys = existingVehicles
                .Where(item => !string.IsNullOrWhiteSpace(item.Placa))
                .Select(item => NormalizePlate(item.Placa!))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingTitleYearKeys = existingVehicles
                .Select(item => BuildTitleYearKey(item.Titulo, item.Ano))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var createdPlateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var createdTitleYearKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in itens)
            {
                var titulo = Truncate((item.Veiculos ?? string.Empty).Trim(), 150);
                if (string.IsNullOrWhiteSpace(titulo))
                {
                    status.Ignorados++;
                    status.Processados++;
                    continue;
                }

                var placaNormalizada = NormalizePlate(item.Placa ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(placaNormalizada))
                {
                    if (existingPlateKeys.Contains(placaNormalizada) || createdPlateKeys.Contains(placaNormalizada))
                    {
                        status.Ignorados++;
                        status.Processados++;
                        continue;
                    }
                }
                else
                {
                    var titleYearKey = BuildTitleYearKey(titulo, item.Ano);
                    if (!string.IsNullOrWhiteSpace(titleYearKey) &&
                        (existingTitleYearKeys.Contains(titleYearKey) || createdTitleYearKeys.Contains(titleYearKey)))
                    {
                        status.Ignorados++;
                        status.Processados++;
                        continue;
                    }
                }

                var marcaId = ResolveMarcaId(titulo, marcas, semMarca.Id);

                var veiculo = new Veiculo
                {
                    LojaId = lojaId.Value,
                    MarcaId = marcaId,
                    Titulo = titulo,
                    Modelo = Truncate(ExtractModelo(titulo), 100),
                    AnoFabricacao = item.Ano,
                    AnoModelo = item.Ano,
                    Cor = Truncate((item.Cor ?? string.Empty).Trim(), 30),
                    Combustivel = Truncate((item.Combustivel ?? string.Empty).Trim(), 30),
                    Quilometragem = item.Km,
                    Placa = string.IsNullOrWhiteSpace(placaNormalizada) ? null : Truncate(placaNormalizada, 20),
                    PrecoVenda = item.Preco,
                    AceitaTroca = false,
                    Financiavel = false,
                    Destaque = false,
                    Seminovo = true,
                    MotoEletrica = false,
                    ImportadoMidia = false,
                    Ativo = true,
                    Vendido = false,
                    DataCadastro = DateTime.Now,
                    DataAtualizacao = DateTime.Now
                };

                db.Veiculos.Add(veiculo);
                status.Cadastrados++;
                status.Processados++;

                if (!string.IsNullOrWhiteSpace(placaNormalizada))
                {
                    createdPlateKeys.Add(placaNormalizada);
                }
                else
                {
                    var titleYearKey = BuildTitleYearKey(titulo, item.Ano);
                    if (!string.IsNullOrWhiteSpace(titleYearKey))
                    {
                        createdTitleYearKeys.Add(titleYearKey);
                    }
                }
            }

            if (status.Cadastrados > 0)
            {
                await db.SaveChangesAsync();
            }

            status.Message = $"Concluido: {status.Cadastrados} cadastrado(s), {status.Ignorados} ignorado(s).";
        }
        catch (Exception ex)
        {
            status.Message = $"Falha no cadastro: {ex.Message}";
        }
        finally
        {
            status.Running = false;
            status.Completed = true;
        }
    }

    public async Task<IActionResult> OnPostCorrigirAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || !DownloadStore.TryGetValue(token, out var payload))
        {
            TempData["Error"] = "Resultado da conferencia nao encontrado. Processe novamente.";
            return RedirectToPage();
        }

        var ids = payload.CorrecoesPorPlaca
            .Select(item => item.VeiculoId)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            TempData["Success"] = "Nao ha divergencias por placa para corrigir.";
            return RedirectToPage(new { token });
        }

        var veiculos = await _db.Veiculos
            .Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var totalCorrigidos = 0;
        foreach (var correcao in payload.CorrecoesPorPlaca)
        {
            if (!veiculos.TryGetValue(correcao.VeiculoId, out var veiculo))
            {
                continue;
            }

            ApplyCorrecao(veiculo, correcao);
            totalCorrigidos++;
        }

        if (totalCorrigidos > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        TempData["Success"] = $"{totalCorrigidos} veiculo(s) corrigido(s) com base na planilha.";
        return RedirectToPage(new { token });
    }

    private void FillFromPayload(string token, DownloadPayload payload)
    {
        DownloadToken = token;
        DownloadFileName = payload.FileName;
        TotalCarrosCadastrados = payload.TotalCadastrados;
        TotalCarrosNaoEncontrados = payload.TotalNaoEncontrados;
        TotalCarrosDiferentes = payload.TotalDivergencias;
    }

    private static void ApplyCorrecao(Domain.Veiculo veiculo, ConferenciaCorrecaoItem correcao)
    {
        if (!string.IsNullOrWhiteSpace(correcao.Veiculos))
        {
            veiculo.Titulo = Truncate(correcao.Veiculos.Trim(), 150);
        }

        if (correcao.Ano.HasValue)
        {
            veiculo.AnoModelo = correcao.Ano.Value;
        }

        if (!string.IsNullOrWhiteSpace(correcao.Cor))
        {
            veiculo.Cor = Truncate(correcao.Cor.Trim(), 30);
        }

        if (!string.IsNullOrWhiteSpace(correcao.Combustivel))
        {
            veiculo.Combustivel = Truncate(correcao.Combustivel.Trim(), 30);
        }

        if (correcao.Preco.HasValue)
        {
            veiculo.PrecoVenda = correcao.Preco.Value;
        }

        if (correcao.Km.HasValue)
        {
            veiculo.Quilometragem = correcao.Km.Value;
        }

        veiculo.DataAtualizacao = DateTime.Now;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }

    private static int ResolveMarcaId(string titulo, IReadOnlyCollection<Marca> marcas, int fallbackMarcaId)
    {
        var normalizedTitle = NormalizeText(titulo);
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return fallbackMarcaId;
        }

        var match = marcas
            .Select(item => new { item.Id, Nome = item.Nome, Normalized = NormalizeText(item.Nome) })
            .Where(item => !string.IsNullOrWhiteSpace(item.Normalized))
            .OrderByDescending(item => item.Normalized.Length)
            .FirstOrDefault(item =>
                normalizedTitle.Equals(item.Normalized, StringComparison.OrdinalIgnoreCase) ||
                normalizedTitle.StartsWith(item.Normalized + " ", StringComparison.OrdinalIgnoreCase));

        return match?.Id ?? fallbackMarcaId;
    }

    private static string ExtractModelo(string titulo)
    {
        var value = (titulo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var firstSpace = value.IndexOf(' ');
        if (firstSpace < 0 || firstSpace + 1 >= value.Length)
        {
            return value;
        }

        return value[(firstSpace + 1)..].Trim();
    }

    private static string NormalizePlate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToUpperInvariant(ch));
            }
        }

        return sb.ToString();
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim().ToUpperInvariant();
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ')
            {
                sb.Append(ch);
            }
        }

        return string.Join(" ", sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string BuildTitleYearKey(string? titulo, int? ano)
    {
        var normalizedTitle = NormalizeText(titulo);
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return string.Empty;
        }

        return $"{normalizedTitle}|{ano?.ToString() ?? "0"}";
    }

    private sealed record DownloadPayload(
        byte[] Content,
        string FileName,
        byte[] NotFoundContent,
        string NotFoundFileName,
        int TotalCadastrados,
        int TotalNaoEncontrados,
        int TotalDivergencias,
        IReadOnlyList<ConferenciaNaoEncontradoCadastroItem> CadastrosNaoEncontrados,
        IReadOnlyList<ConferenciaCorrecaoItem> CorrecoesPorPlaca);

    private sealed class CadastroJobStatus
    {
        public bool Running { get; set; }
        public bool Completed { get; set; }
        public int Total { get; set; }
        public int Processados { get; set; }
        public int Cadastrados { get; set; }
        public int Ignorados { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
