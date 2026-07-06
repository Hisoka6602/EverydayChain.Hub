using System.IO.Compression;
using System.Security;
using System.Text;

namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 定义当前类型。
/// </summary>
public static class SimpleXlsxBuilder
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public static byte[] BuildSingleSheet(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(
                archive,
                "[Content_Types].xml",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);

            WriteEntry(
                archive,
                "_rels/.rels",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);

            WriteEntry(
                archive,
                "xl/workbook.xml",
                $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                    <sheet name="{EscapeXmlAttribute(NormalizeSheetName(sheetName))}" sheetId="1" r:id="rId1"/>
                  </sheets>
                </workbook>
                """);

            WriteEntry(
                archive,
                "xl/_rels/workbook.xml.rels",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);

            WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(headers, rows));
        }

        return stream.ToArray();
    }

    private static string BuildWorksheetXml(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.AppendLine("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
        builder.AppendLine("""  <sheetData>""");

        AppendRow(builder, 1, headers);
        for (var index = 0; index < rows.Count; index++)
        {
            AppendRow(builder, index + 2, rows[index]);
        }

        builder.AppendLine("""  </sheetData>""");
        builder.AppendLine("""</worksheet>""");
        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, int rowNumber, IReadOnlyList<string?> cells)
    {
        builder.AppendLine($"""    <row r="{rowNumber}">""");
        for (var columnIndex = 0; columnIndex < cells.Count; columnIndex++)
        {
            var cellRef = $"{ToColumnName(columnIndex)}{rowNumber}";
            var value = EscapeXmlText(cells[columnIndex] ?? string.Empty);
            builder.AppendLine($"""      <c r="{cellRef}" t="inlineStr"><is><t xml:space="preserve">{value}</t></is></c>""");
        }

        builder.AppendLine("""    </row>""");
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static string NormalizeSheetName(string sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            return "Sheet1";
        }

        var invalidChars = new HashSet<char>(['\\', '/', '?', '*', '[', ']', ':']);
        var normalized = new string(sheetName
            .Trim()
            .Select(character => invalidChars.Contains(character) ? '_' : character)
            .ToArray());
        if (normalized.Length > 31)
        {
            normalized = normalized[..31];
        }

        return string.IsNullOrWhiteSpace(normalized) ? "Sheet1" : normalized;
    }

    private static string ToColumnName(int zeroBasedIndex)
    {
        var index = zeroBasedIndex + 1;
        var builder = new StringBuilder();
        while (index > 0)
        {
            index--;
            builder.Insert(0, (char)('A' + (index % 26)));
            index /= 26;
        }

        return builder.ToString();
    }

    private static string EscapeXmlText(string value)
    {
        return SecurityElement.Escape(value) ?? string.Empty;
    }

    private static string EscapeXmlAttribute(string value)
    {
        return EscapeXmlText(value).Replace("\"", "&quot;", StringComparison.Ordinal);
    }
}
