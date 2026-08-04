namespace BaiduShareTool.Export;

/// <summary>Excel 导出行。</summary>
public sealed record ExportRow(string FileName, string ShareUrl);

/// <summary>导出参数。</summary>
public sealed record ExportOptions(string OutputPath, bool OverwriteExisting = true);

/// <summary>导出进度。</summary>
public sealed record ExportProgress(long WrittenRows, string? CurrentFileName);

/// <summary>Excel 流式导出接口。</summary>
public interface IExcelExportService
{
    Task ExportAsync(
        IAsyncEnumerable<ExportRow> rows,
        ExportOptions options,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
