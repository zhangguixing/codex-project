using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace BaiduShareTool.Export;

/// <summary>使用 OpenXmlWriter 流式写入 Excel，适合大文件导出。</summary>
public sealed class OpenXmlExcelExportService : IExcelExportService
{
    public async Task ExportAsync(
        IAsyncEnumerable<ExportRow> rows,
        ExportOptions options,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (File.Exists(options.OutputPath) && !options.OverwriteExisting) throw new IOException($"文件已存在：{options.OutputPath}");

        var temporaryPath = options.OutputPath + ".tmp";
        if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        try
        {
            using (var document = SpreadsheetDocument.Create(temporaryPath, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                using (var writer = OpenXmlWriter.Create(worksheetPart))
                {
                    writer.WriteStartElement(new Worksheet());
                    writer.WriteStartElement(new SheetData());
                    WriteRow(writer, "文件名", "分享链接");
                    var count = 0L;
                    await foreach (var row in rows.WithCancellation(cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        WriteRow(writer, RemoveLastExtension(row.FileName), row.ShareUrl);
                        count++;
                        progress?.Report(new ExportProgress(count, row.FileName));
                    }
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                worksheetPart.Worksheet.Save();
                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet { Name = "分享链接", SheetId = 1, Id = workbookPart.GetIdOfPart(worksheetPart) });
                workbookPart.Workbook.Save();
            }
            File.Move(temporaryPath, options.OutputPath, true);
        }
        catch
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            throw;
        }
    }

    /// <summary>只移除最后一个扩展名，例如 abc.tar.gz 变为 abc.tar。</summary>
    public static string RemoveLastExtension(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var extensionStart = name.LastIndexOf('.');
        return extensionStart <= 0 ? name : name[..extensionStart];
    }

    private static void WriteRow(OpenXmlWriter writer, string fileName, string shareUrl)
    {
        writer.WriteStartElement(new Row());
        WriteCell(writer, fileName);
        WriteCell(writer, shareUrl);
        writer.WriteEndElement();
    }

    private static void WriteCell(OpenXmlWriter writer, string value)
    {
        writer.WriteStartElement(new Cell { DataType = CellValues.InlineString });
        writer.WriteElement(new InlineString(new Text(value ?? string.Empty)));
        writer.WriteEndElement();
    }
}
