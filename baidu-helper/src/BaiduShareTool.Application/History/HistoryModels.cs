namespace BaiduShareTool.Application.History;
public sealed record ExportHistoryRecord(long Id, string TaskId, DateTimeOffset ExportTime, string RootPath, int FileCount, int SuccessCount, int FailedCount, string FilePath)
{
    public DateTime LocalExportTime => ExportTime.ToLocalTime().DateTime;
}
public interface IHistoryService
{
    Task<IReadOnlyList<ExportHistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ExportHistoryRecord record, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
