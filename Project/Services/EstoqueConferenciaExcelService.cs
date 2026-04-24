using Data;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Project.Services;

public interface IEstoqueConferenciaExcelService
{
    Task<ConferenciaEstoqueArquivoResult> ProcessarAsync(Stream excelStream, CancellationToken cancellationToken = default);
}

public sealed class EstoqueConferenciaExcelService : IEstoqueConferenciaExcelService
{
    private static readonly Regex MultiSpaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex NonDigitsRegex = new(@"[^\d]", RegexOptions.Compiled);
    private readonly ApplicationDbContext _db;

    public EstoqueConferenciaExcelService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ConferenciaEstoqueArquivoResult> ProcessarAsync(Stream excelStream, CancellationToken cancellationToken = default)
    {
        var linhas = LerLinhasValidas(excelStream);
        if (linhas.Count == 0)
        {
            var vazio = GerarExcelResultado([], []);
            return new ConferenciaEstoqueArquivoResult(
                vazio,
                "conferencia_estoque_vazio.xlsx",
                vazio,
                "conferencia_estoque_nao_encontrados_vazio.xlsx",
                0,
                0,
                0,
                0,
                [],
                []);
        }

        var veiculosSite = await CarregarVeiculosSiteAsync(cancellationToken);
        var resultado = ConferirLinhas(linhas, veiculosSite);
        var colunas = ConsolidarColunasOriginais(linhas);
        var arquivo = GerarExcelResultado(resultado, colunas);
        var nomeArquivo = $"conferencia_estoque_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        var somenteNaoEncontrados = resultado
            .Where(item => item.Status == "NAO_ENCONTRADO")
            .ToList();
        var arquivoNaoEncontrados = GerarExcelResultado(somenteNaoEncontrados, colunas);
        var nomeArquivoNaoEncontrados = $"conferencia_estoque_nao_encontrados_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        var totalNaoEncontrados = resultado.Count(item => item.Status == "NAO_ENCONTRADO");
        var totalDivergencias = resultado.Count(item => item.Status == "DIVERGENCIA");
        var totalCadastrados = resultado.Count - totalNaoEncontrados;
        var cadastrosNaoEncontrados = somenteNaoEncontrados
            .Select(item =>
            {
                var linha = item.Linha;
                return new ConferenciaNaoEncontradoCadastroItem(
                    linha.GetCanonical("VEICULOS"),
                    ParseAno(linha.GetCanonical("ANO")),
                    linha.GetCanonical("COR"),
                    linha.GetCanonical("COMBUSTIVEL"),
                    ParseDecimal(linha.GetCanonical("PRECO")),
                    ParseInt(linha.GetCanonical("KM")),
                    linha.GetCanonical("PLACA"));
            })
            .ToList();
        var correcoesPorPlaca = resultado
            .Where(item => item.Status == "DIVERGENCIA" && item.MatchedByPlate && item.MatchId.HasValue)
            .GroupBy(item => item.MatchId!.Value)
            .Select(group =>
            {
                var linha = group.First().Linha;
                return new ConferenciaCorrecaoItem(
                    group.Key,
                    linha.GetCanonical("VEICULOS"),
                    ParseAno(linha.GetCanonical("ANO")),
                    linha.GetCanonical("COR"),
                    linha.GetCanonical("COMBUSTIVEL"),
                    ParseDecimal(linha.GetCanonical("PRECO")),
                    ParseInt(linha.GetCanonical("KM")),
                    linha.GetCanonical("PLACA"));
            })
            .ToList();

        return new ConferenciaEstoqueArquivoResult(
            arquivo,
            nomeArquivo,
            arquivoNaoEncontrados,
            nomeArquivoNaoEncontrados,
            resultado.Count,
            totalCadastrados,
            totalNaoEncontrados,
            totalDivergencias,
            cadastrosNaoEncontrados,
            correcoesPorPlaca);
    }

    private static List<EstoqueLinhaImportada> LerLinhasValidas(Stream excelStream)
    {
        using var ms = new MemoryStream();
        excelStream.CopyTo(ms);
        ms.Position = 0;

        using var document = SpreadsheetDocument.Open(ms, false);
        var workbookPart = document.WorkbookPart;
        if (workbookPart == null)
        {
            return [];
        }

        var firstSheet = workbookPart.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault();
        if (firstSheet == null)
        {
            return [];
        }

        var worksheetPart = workbookPart.GetPartById(firstSheet.Id!) as WorksheetPart;
        var rows = worksheetPart?.Worksheet.GetFirstChild<SheetData>()?.Elements<Row>() ?? [];

        HeaderContext? currentHeader = null;
        var output = new List<EstoqueLinhaImportada>();

        foreach (var row in rows)
        {
            var valuesByColumn = LerValoresLinha(workbookPart, row);
            if (valuesByColumn.Count == 0)
            {
                continue;
            }

            if (TryBuildHeader(valuesByColumn, out var header))
            {
                currentHeader = header;
                continue;
            }

            if (currentHeader == null)
            {
                continue;
            }

            var rowData = currentHeader.Columns
                .Select(col => new LinhaValor(col.OriginalName, col.CanonicalName, valuesByColumn.GetValueOrDefault(col.ColumnIndex) ?? string.Empty))
                .ToList();

            if (!IsVehicleRow(rowData))
            {
                continue;
            }

            var originalValues = rowData
                .ToDictionary(item => item.OriginalName, item => item.Value, StringComparer.OrdinalIgnoreCase);

            var canonical = rowData
                .Where(item => !string.IsNullOrWhiteSpace(item.CanonicalName))
                .GroupBy(item => item.CanonicalName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);

            output.Add(new EstoqueLinhaImportada
            {
                OriginalColumnOrder = rowData.Select(item => item.OriginalName).ToList(),
                OriginalValues = originalValues,
                CanonicalValues = canonical
            });
        }

        return output;
    }

    private async Task<List<SiteVehicleItem>> CarregarVeiculosSiteAsync(CancellationToken cancellationToken)
    {
        return await _db.Veiculos
            .AsNoTracking()
            .Where(veiculo => veiculo.Ativo && !veiculo.Vendido)
            .Select(veiculo => new SiteVehicleItem
            {
                Id = veiculo.Id,
                Placa = veiculo.Placa,
                Titulo = veiculo.Titulo,
                Marca = veiculo.Marca != null ? veiculo.Marca.Nome : null,
                Modelo = veiculo.Modelo,
                Versao = veiculo.Versao,
                Ano = veiculo.AnoModelo ?? veiculo.AnoFabricacao,
                Cor = veiculo.Cor,
                Combustivel = veiculo.Combustivel,
                Preco = veiculo.PrecoVenda,
                Km = veiculo.Quilometragem
            })
            .ToListAsync(cancellationToken);
    }

    private static List<ConferenciaResultadoLinha> ConferirLinhas(
        IReadOnlyCollection<EstoqueLinhaImportada> linhas,
        IReadOnlyCollection<SiteVehicleItem> veiculosSite)
    {
        var vehiclesByPlate = veiculosSite
            .Where(item => !string.IsNullOrWhiteSpace(item.Placa))
            .GroupBy(item => NormalizarPlaca(item.Placa))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var vehiclesByName = veiculosSite
            .GroupBy(item => NormalizarTexto($"{item.Marca} {item.Modelo} {item.Versao} {item.Titulo}"))
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var resultado = new List<ConferenciaResultadoLinha>();

        foreach (var linha in linhas)
        {
            var placaArquivo = linha.GetCanonical("PLACA");
            var veiculoArquivo = linha.GetCanonical("VEICULOS");
            var anoArquivo = ParseAno(linha.GetCanonical("ANO"));
            var corArquivo = linha.GetCanonical("COR");
            var combustivelArquivo = linha.GetCanonical("COMBUSTIVEL");
            var precoArquivo = ParseDecimal(linha.GetCanonical("PRECO"));
            var kmArquivo = ParseInt(linha.GetCanonical("KM"));

            SiteVehicleItem? match = null;

            var placaNormalizada = NormalizarPlaca(placaArquivo);
            var placaInformada = !string.IsNullOrWhiteSpace(placaNormalizada);

            if (placaInformada && vehiclesByPlate.TryGetValue(placaNormalizada, out var byPlate))
            {
                match = byPlate;
            }

            // Regra: se a planilha trouxe placa, valida exclusivamente por placa.
            // Fallback por nome/modelo apenas quando a placa nao foi informada.
            if (match == null && !placaInformada)
            {
                var nomeNormalizado = NormalizarTexto(veiculoArquivo);
                if (!string.IsNullOrWhiteSpace(nomeNormalizado))
                {
                    if (vehiclesByName.TryGetValue(nomeNormalizado, out var byName))
                    {
                        match = byName;
                    }
                    else
                    {
                        match = veiculosSite.FirstOrDefault(item =>
                        {
                            var siteName = NormalizarTexto($"{item.Marca} {item.Modelo} {item.Versao} {item.Titulo}");
                            if (siteName.Length < 5 || nomeNormalizado.Length < 5)
                            {
                                return false;
                            }

                            return siteName.Contains(nomeNormalizado, StringComparison.Ordinal) ||
                                   nomeNormalizado.Contains(siteName, StringComparison.Ordinal);
                        });
                    }
                }
            }

            if (match == null)
            {
                resultado.Add(new ConferenciaResultadoLinha
                {
                    Status = "NAO_ENCONTRADO",
                    IdSite = string.Empty,
                    Divergencias = "NA",
                    MatchId = null,
                    MatchedByPlate = false,
                    Linha = linha
                });
                continue;
            }

            var divergencias = new List<string>();
            var siteVehicleName = $"{match.Marca} {match.Modelo} {match.Versao} {match.Titulo}";

            if (!TextoCompativel(veiculoArquivo, siteVehicleName))
            {
                divergencias.Add("VEICULOS");
            }

            if (!IntCompativel(anoArquivo, match.Ano))
            {
                divergencias.Add("ANO");
            }

            if (!TextoCompativel(corArquivo, match.Cor))
            {
                divergencias.Add("COR");
            }

            if (!DecimalCompativel(precoArquivo, match.Preco))
            {
                divergencias.Add("PRECO");
            }

            if (!TextoCompativel(combustivelArquivo, match.Combustivel))
            {
                divergencias.Add("COMBUSTIVEL");
            }

            if (!IntCompativel(kmArquivo, match.Km))
            {
                divergencias.Add("KM");
            }

            resultado.Add(new ConferenciaResultadoLinha
            {
                Status = divergencias.Count == 0 ? "OK" : "DIVERGENCIA",
                IdSite = match.Id.ToString(CultureInfo.InvariantCulture),
                Divergencias = divergencias.Count == 0 ? string.Empty : string.Join(",", divergencias),
                MatchId = match.Id,
                MatchedByPlate = placaInformada,
                Linha = linha
            });
        }

        return resultado;
    }

    private static List<string> ConsolidarColunasOriginais(IReadOnlyCollection<EstoqueLinhaImportada> linhas)
    {
        var output = new List<string>();
        foreach (var linha in linhas)
        {
            foreach (var column in linha.OriginalColumnOrder)
            {
                if (output.Contains(column, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                output.Add(column);
            }
        }

        return output;
    }

    private static byte[] GerarExcelResultado(
        IReadOnlyCollection<ConferenciaResultadoLinha> linhas,
        IReadOnlyCollection<string> colunasOriginais)
    {
        using var ms = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Conferencia"
            });

            var header = new Row();
            header.Append(CreateTextCell("STATUS"));
            header.Append(CreateTextCell("ID_SITE"));
            header.Append(CreateTextCell("DIVERGENCIAS"));
            foreach (var coluna in colunasOriginais)
            {
                header.Append(CreateTextCell(coluna));
            }

            sheetData.Append(header);

            foreach (var item in linhas)
            {
                var row = new Row();
                row.Append(CreateTextCell(item.Status));
                row.Append(CreateTextCell(item.IdSite));
                row.Append(CreateTextCell(item.Divergencias));

                foreach (var coluna in colunasOriginais)
                {
                    row.Append(CreateTextCell(item.Linha.OriginalValues.GetValueOrDefault(coluna) ?? string.Empty));
                }

                sheetData.Append(row);
            }

            workbookPart.Workbook.Save();
        }

        return ms.ToArray();
    }

    private static Cell CreateTextCell(string value)
    {
        return new Cell
        {
            DataType = CellValues.InlineString,
            InlineString = new InlineString(new Text(value ?? string.Empty))
        };
    }

    private static Dictionary<int, string> LerValoresLinha(WorkbookPart workbookPart, Row row)
    {
        var output = new Dictionary<int, string>();
        foreach (var cell in row.Elements<Cell>())
        {
            var index = GetColumnIndex(cell.CellReference);
            output[index] = GetCellValue(workbookPart, cell).Trim();
        }

        return output;
    }

    private static bool TryBuildHeader(Dictionary<int, string> valuesByColumn, out HeaderContext header)
    {
        header = new HeaderContext();
        if (valuesByColumn.Count == 0)
        {
            return false;
        }

        var recognized = 0;
        var hasVehicleColumn = false;
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in valuesByColumn.OrderBy(item => item.Key))
        {
            var original = pair.Value.Trim();
            if (string.IsNullOrWhiteSpace(original))
            {
                continue;
            }

            var canonical = ResolveCanonicalHeader(original);
            if (!string.IsNullOrWhiteSpace(canonical))
            {
                recognized++;
                if (string.Equals(canonical, "VEICULOS", StringComparison.OrdinalIgnoreCase))
                {
                    hasVehicleColumn = true;
                }
            }

            var originalName = original;
            if (!usedNames.Add(originalName))
            {
                var suffix = 2;
                while (!usedNames.Add($"{originalName}_{suffix}"))
                {
                    suffix++;
                }

                originalName = $"{originalName}_{suffix}";
            }

            header.Columns.Add(new HeaderColumn(pair.Key, originalName, canonical));
        }

        var isHeader = recognized >= 2 && hasVehicleColumn;
        if (!isHeader)
        {
            header = new HeaderContext();
            return false;
        }

        return true;
    }

    private static bool IsVehicleRow(IReadOnlyCollection<LinhaValor> rowData)
    {
        if (rowData.Count == 0 || rowData.All(item => string.IsNullOrWhiteSpace(item.Value)))
        {
            return false;
        }

        var veiculos = rowData.FirstOrDefault(item => string.Equals(item.CanonicalName, "VEICULOS", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        var placa = rowData.FirstOrDefault(item => string.Equals(item.CanonicalName, "PLACA", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        var ano = rowData.FirstOrDefault(item => string.Equals(item.CanonicalName, "ANO", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        var preco = rowData.FirstOrDefault(item => string.Equals(item.CanonicalName, "PRECO", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        var km = rowData.FirstOrDefault(item => string.Equals(item.CanonicalName, "KM", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        var combustivel = rowData.FirstOrDefault(item => string.Equals(item.CanonicalName, "COMBUSTIVEL", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(veiculos))
        {
            return false;
        }

        var hasAnyIdentifier =
            !string.IsNullOrWhiteSpace(placa) ||
            !string.IsNullOrWhiteSpace(ano) ||
            !string.IsNullOrWhiteSpace(preco) ||
            !string.IsNullOrWhiteSpace(km) ||
            !string.IsNullOrWhiteSpace(combustivel);

        if (!hasAnyIdentifier)
        {
            return false;
        }

        var veiculoToken = NormalizarTexto(veiculos);
        if (IsSectionTitleToken(veiculoToken))
        {
            return false;
        }

        return true;
    }

    private static bool IsSectionTitleToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        var knownTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MOTOS", "IMPORTADOS", "NACIONAIS", "HATCH", "SEDAN", "SUV", "PICKUP",
            "HONDA", "FIAT", "CHEVROLET", "VW", "VOLKSWAGEN", "TOYOTA", "HYUNDAI",
            "RENAULT", "NISSAN", "FORD", "JEEP", "PEUGEOT", "CITROEN", "AUDI", "BMW",
            "MERCEDES", "KIA", "CHERY", "BYD", "GWM"
        };

        if (knownTitles.Contains(token))
        {
            return true;
        }

        return false;
    }

    private static string ResolveCanonicalHeader(string value)
    {
        var token = NormalizarTexto(value);
        return token switch
        {
            "VEICULOS" or "VEICULO" or "MODELO" => "VEICULOS",
            "ANO" => "ANO",
            "COR" => "COR",
            "PLACA" => "PLACA",
            "PRECO" or "VALOR" or "PRECO R$" or "PRECO RS" => "PRECO",
            "COMBUSTIVEL" => "COMBUSTIVEL",
            "ENTRADA" => "ENTRADA",
            "PATIO" => "PATIO",
            "KM" or "QUILOMETRAGEM" => "KM",
            "SITE" => "SITE",
            _ => string.Empty
        };
    }

    private static bool TextoCompativel(string? valorArquivo, string? valorSite)
    {
        if (string.IsNullOrWhiteSpace(valorArquivo))
        {
            return true;
        }

        var a = NormalizarTexto(valorArquivo);
        var b = NormalizarTexto(valorSite);
        if (string.IsNullOrWhiteSpace(b))
        {
            return false;
        }

        return a == b || a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal);
    }

    private static bool IntCompativel(int? valorArquivo, int? valorSite)
    {
        if (!valorArquivo.HasValue)
        {
            return true;
        }

        if (!valorSite.HasValue)
        {
            return false;
        }

        return valorArquivo.Value == valorSite.Value;
    }

    private static bool DecimalCompativel(decimal? valorArquivo, decimal? valorSite)
    {
        if (!valorArquivo.HasValue)
        {
            return true;
        }

        if (!valorSite.HasValue)
        {
            return false;
        }

        return Math.Abs(valorArquivo.Value - valorSite.Value) <= 1m;
    }

    private static int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = NonDigitsRegex.Replace(value, string.Empty);
        if (string.IsNullOrWhiteSpace(digits))
        {
            return null;
        }

        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var output)
            ? output
            : null;
    }

    private static int? ParseAno(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = Regex.Match(value, @"\b(19|20)\d{2}\b");
        if (match.Success && int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ano))
        {
            return ano;
        }

        return ParseInt(value);
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = value
            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty);

        if (sanitized.Contains(',') && sanitized.Contains('.'))
        {
            if (sanitized.LastIndexOf(',') > sanitized.LastIndexOf('.'))
            {
                sanitized = sanitized.Replace(".", string.Empty).Replace(",", ".");
            }
            else
            {
                sanitized = sanitized.Replace(",", string.Empty);
            }
        }
        else if (sanitized.Contains(','))
        {
            sanitized = sanitized.Replace(".", string.Empty).Replace(",", ".");
        }

        return decimal.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out var output)
            ? output
            : null;
    }

    private static string NormalizarTexto(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = MultiSpaceRegex.Replace(value.Trim(), " ");
        var normalized = trimmed.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    private static string NormalizarPlaca(string? value)
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

    private static string GetCellValue(WorkbookPart workbookPart, Cell cell)
    {
        if (cell.CellValue == null && cell.InlineString == null)
        {
            return string.Empty;
        }

        if (cell.DataType == null)
        {
            return cell.CellValue?.InnerText ?? string.Empty;
        }

        var dataType = cell.DataType.Value;

        if (dataType == CellValues.SharedString)
        {
            return GetSharedString(workbookPart, cell.CellValue?.InnerText);
        }

        if (dataType == CellValues.InlineString)
        {
            return cell.InlineString?.InnerText ?? string.Empty;
        }

        return cell.CellValue?.InnerText ?? string.Empty;
    }

    private static string GetSharedString(WorkbookPart workbookPart, string? indexText)
    {
        if (string.IsNullOrWhiteSpace(indexText) || !int.TryParse(indexText, out var index))
        {
            return string.Empty;
        }

        var table = workbookPart.SharedStringTablePart?.SharedStringTable;
        if (table == null)
        {
            return string.Empty;
        }

        if (index < 0 || index >= table.Count())
        {
            return string.Empty;
        }

        return table.ElementAt(index).InnerText;
    }

    private static int GetColumnIndex(StringValue? cellReference)
    {
        if (cellReference == null)
        {
            return 1;
        }

        var reference = cellReference.Value ?? string.Empty;
        var index = 0;
        foreach (var ch in reference)
        {
            if (!char.IsLetter(ch))
            {
                break;
            }

            index = (index * 26) + (char.ToUpperInvariant(ch) - 'A' + 1);
        }

        return index == 0 ? 1 : index;
    }

    private sealed record HeaderColumn(int ColumnIndex, string OriginalName, string CanonicalName);

    private sealed class HeaderContext
    {
        public List<HeaderColumn> Columns { get; } = [];
    }

    private sealed record LinhaValor(string OriginalName, string CanonicalName, string Value);

    private sealed class SiteVehicleItem
    {
        public int Id { get; init; }
        public string? Placa { get; init; }
        public string? Titulo { get; init; }
        public string? Marca { get; init; }
        public string? Modelo { get; init; }
        public string? Versao { get; init; }
        public int? Ano { get; init; }
        public string? Cor { get; init; }
        public string? Combustivel { get; init; }
        public decimal? Preco { get; init; }
        public int? Km { get; init; }
    }

    private sealed class EstoqueLinhaImportada
    {
        public IReadOnlyList<string> OriginalColumnOrder { get; init; } = [];
        public Dictionary<string, string> OriginalValues { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> CanonicalValues { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        public string GetCanonical(string key)
        {
            return CanonicalValues.GetValueOrDefault(key) ?? string.Empty;
        }
    }

    private sealed class ConferenciaResultadoLinha
    {
        public string Status { get; init; } = string.Empty;
        public string IdSite { get; init; } = string.Empty;
        public string Divergencias { get; init; } = string.Empty;
        public int? MatchId { get; init; }
        public bool MatchedByPlate { get; init; }
        public EstoqueLinhaImportada Linha { get; init; } = new();
    }
}

public sealed record ConferenciaEstoqueArquivoResult(
    byte[] Content,
    string FileName,
    byte[] NotFoundContent,
    string NotFoundFileName,
    int TotalProcessados,
    int TotalCadastrados,
    int TotalNaoEncontrados,
    int TotalDivergencias,
    IReadOnlyList<ConferenciaNaoEncontradoCadastroItem> CadastrosNaoEncontrados,
    IReadOnlyList<ConferenciaCorrecaoItem> CorrecoesPorPlaca);

public sealed record ConferenciaNaoEncontradoCadastroItem(
    string? Veiculos,
    int? Ano,
    string? Cor,
    string? Combustivel,
    decimal? Preco,
    int? Km,
    string? Placa);

public sealed record ConferenciaCorrecaoItem(
    int VeiculoId,
    string? Veiculos,
    int? Ano,
    string? Cor,
    string? Combustivel,
    decimal? Preco,
    int? Km,
    string? Placa);
