using BaiduShareTool.Application.History;
using Microsoft.Data.Sqlite;
namespace BaiduShareTool.Infrastructure.History;
public sealed class SqliteHistoryService : IHistoryService
{
    private readonly string cs = $"Data Source={Path.Combine(AppContext.BaseDirectory, "data", "app.db")}";
    public async Task<IReadOnlyList<ExportHistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<ExportHistoryRecord>(); await using var c = new SqliteConnection(cs); await c.OpenAsync(cancellationToken); await using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT id, task_id, export_time, root_path, file_count, success_count, failed_count, file_path FROM export_history ORDER BY export_time DESC"; await using var r = await cmd.ExecuteReaderAsync(cancellationToken); while (await r.ReadAsync(cancellationToken)) result.Add(new ExportHistoryRecord(r.GetInt64(0), r.GetString(1), DateTimeOffset.Parse(r.GetString(2)), r.GetString(3), r.GetInt32(4), r.GetInt32(5), r.GetInt32(6), r.GetString(7))); return result;
    }
    public async Task AddAsync(ExportHistoryRecord record, CancellationToken cancellationToken = default)
    {
        await using var c = new SqliteConnection(cs); await c.OpenAsync(cancellationToken); await using var cmd = c.CreateCommand(); cmd.CommandText = "INSERT INTO export_history (task_id, export_time, root_path, file_count, success_count, failed_count, file_path, status) VALUES ($task, $time, $root, $count, $success, $failed, $path, 'Completed')"; cmd.Parameters.AddWithValue("$task", record.TaskId); cmd.Parameters.AddWithValue("$time", record.ExportTime.ToString("O")); cmd.Parameters.AddWithValue("$root", record.RootPath); cmd.Parameters.AddWithValue("$count", record.FileCount); cmd.Parameters.AddWithValue("$success", record.SuccessCount); cmd.Parameters.AddWithValue("$failed", record.FailedCount); cmd.Parameters.AddWithValue("$path", record.FilePath); await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var c = new SqliteConnection(cs); await c.OpenAsync(cancellationToken);
        await using var cmd = c.CreateCommand(); cmd.CommandText = "DELETE FROM export_history WHERE id = $id"; cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
