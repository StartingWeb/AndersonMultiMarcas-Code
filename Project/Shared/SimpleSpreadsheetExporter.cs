using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Project.Shared;

public static class SimpleSpreadsheetExporter
{
    public static byte[] CreateWorkbook(IReadOnlyList<SpreadsheetSheet> sheets)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypesXml(sheets.Count));
            WriteEntry(archive, "_rels/.rels", RootRelationshipsXml());
            WriteEntry(archive, "xl/workbook.xml", WorkbookXml(sheets));
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml(sheets.Count));
            WriteEntry(archive, "xl/styles.xml", StylesXml());

            for (var index = 0; index < sheets.Count; index++)
            {
                WriteEntry(archive, $"xl/worksheets/sheet{index + 1}.xml", WorksheetXml(sheets[index]));
            }
        }

        return memory.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string ContentTypesXml(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""");
        sb.Append("""<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""");
        sb.Append("""<Default Extension="xml" ContentType="application/xml"/>""");
        sb.Append("""<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>""");
        sb.Append("""<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>""");
        for (var i = 1; i <= sheetCount; i++)
        {
            sb.Append($"""<Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""");
        }

        sb.Append("</Types>");
        return sb.ToString();
    }

    private static string RootRelationshipsXml()
        => """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""";

    private static string WorkbookXml(IReadOnlyList<SpreadsheetSheet> sheets)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>""");
        for (var i = 0; i < sheets.Count; i++)
        {
            sb.Append($"""<sheet name="{XmlEscape(SanitizeSheetName(sheets[i].Name))}" sheetId="{i + 1}" r:id="rId{i + 1}"/>""");
        }

        sb.Append("</sheets></workbook>");
        return sb.ToString();
    }

    private static string WorkbookRelationshipsXml(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""");
        for (var i = 1; i <= sheetCount; i++)
        {
            sb.Append($"""<Relationship Id="rId{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{i}.xml"/>""");
        }

        sb.Append($"""<Relationship Id="rId{sheetCount + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>""");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    private static string StylesXml()
        => """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="1"><font><sz val="11"/><name val="Calibri"/></font></fonts><fills count="1"><fill><patternFill patternType="none"/></fill></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/></cellXfs></styleSheet>""";

    private static string WorksheetXml(SpreadsheetSheet sheet)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");

        for (var rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
        {
            var row = sheet.Rows[rowIndex];
            sb.Append($"""<row r="{rowIndex + 1}">""");
            for (var colIndex = 0; colIndex < row.Count; colIndex++)
            {
                var cellRef = $"{ColumnName(colIndex + 1)}{rowIndex + 1}";
                sb.Append($"""<c r="{cellRef}" t="inlineStr"><is><t>{XmlEscape(row[colIndex] ?? string.Empty)}</t></is></c>""");
            }

            sb.Append("</row>");
        }

        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static string ColumnName(int index)
    {
        var dividend = index;
        var columnName = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    private static string SanitizeSheetName(string name)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var clean = invalid.Aggregate(name, (current, ch) => current.Replace(ch, '-'));
        return string.IsNullOrWhiteSpace(clean) ? "Planilha" : clean[..Math.Min(31, clean.Length)];
    }

    private static string XmlEscape(string value)
        => SecurityElementEscape(value);

    private static string SecurityElementEscape(string value)
    {
        using var stringWriter = new StringWriter();
        using var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment });
        writer.WriteString(value);
        writer.Flush();
        return stringWriter.ToString();
    }
}

public sealed record SpreadsheetSheet(string Name, IReadOnlyList<IReadOnlyList<string?>> Rows);
