using BaiduShareTool.Application.Tasks;
using BaiduShareTool.Providers.Abstractions;
using Microsoft.Data.Sqlite;
using AppTaskStatus = BaiduShareTool.Application.Tasks.TaskStatus;

namespace BaiduShareTool.Infrastructure.Tasks;

/// <summary>SQLite 任务检查点仓储。</summary>
public sealed class SqliteTaskCheckpointService : ITaskCheckpointService
{
    private readonly string connectionString = $"Data Source={Path.Combine(AppContext.BaseDirectory, "data", "app.db")}";

    public async Task<ShareTask> CreateAsync(string providerId, string rootPath, CancellationToken cancellationToken = default)
    {
        var task = new ShareTask(Guid.NewGuid().ToString("N"), providerId, rootPath, AppTaskStatus.Created, 0, 0, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO tasks (task_id, provider, root_path, status, scanned_count, success_count, failed_count, created_at, updated_at) VALUES ($id, $provider, $path, $status, 0, 0, 0, $created, $updated)";
        command.Parameters.AddWithValue("$id", task.TaskId); command.Parameters.AddWithValue("$provider", task.ProviderId); command.Parameters.AddWithValue("$path", task.RootPath); command.Parameters.AddWithValue("$status", task.Status.ToString()); command.Parameters.AddWithValue("$created", task.CreatedAt.ToString("O")); command.Parameters.AddWithValue("$updated", task.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
        return task;
    }

    public async Task<ShareTask?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT task_id, provider, root_path, status, scanned_count, success_count, failed_count, created_at, updated_at FROM tasks WHERE task_id = $id"; command.Parameters.AddWithValue("$id", taskId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken); return await reader.ReadAsync(cancellationToken) ? ReadTask(reader) : null;
    }

    public async Task<IReadOnlyList<ShareTask>> GetResumableAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<ShareTask>(); await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT task_id, provider, root_path, status, scanned_count, success_count, failed_count, created_at, updated_at FROM tasks WHERE status IN ('Created','Scanning','Running','Paused') ORDER BY updated_at DESC";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken); while (await reader.ReadAsync(cancellationToken)) result.Add(ReadTask(reader)); return result;
    }

    public async Task AddItemAsync(string taskId, RemoteFile file, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO task_items (task_id, file_id, file_path, file_name, file_size, status, retry_count, updated_at) VALUES ($task, $id, $path, $name, $size, 'Pending', 0, $updated)";
        command.Parameters.AddWithValue("$task", taskId); command.Parameters.AddWithValue("$id", file.Id); command.Parameters.AddWithValue("$path", file.Path); command.Parameters.AddWithValue("$name", file.Name); command.Parameters.AddWithValue("$size", file.Size); command.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O")); await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateItemAsync(string taskId, string fileId, TaskItemStatus status, string? shareUrl = null, int retryCount = 0, string? error = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE task_items SET status = $status, share_url = $url, retry_count = $retry, last_error = $error, updated_at = $updated WHERE task_id = $task AND file_id = $id";
        command.Parameters.AddWithValue("$status", status.ToString()); command.Parameters.AddWithValue("$url", (object?)shareUrl ?? DBNull.Value); command.Parameters.AddWithValue("$retry", retryCount); command.Parameters.AddWithValue("$error", (object?)error ?? DBNull.Value); command.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O")); command.Parameters.AddWithValue("$task", taskId); command.Parameters.AddWithValue("$id", fileId); await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateTaskStatusAsync(string taskId, AppTaskStatus status, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand(); command.CommandText = "UPDATE tasks SET status = $status, updated_at = $updated WHERE task_id = $id"; command.Parameters.AddWithValue("$status", status.ToString()); command.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O")); command.Parameters.AddWithValue("$id", taskId); await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken) { var connection = new SqliteConnection(connectionString); await connection.OpenAsync(cancellationToken); return connection; }
    private static ShareTask ReadTask(SqliteDataReader reader) => new(reader.GetString(0), reader.GetString(1), reader.GetString(2), Enum.Parse<AppTaskStatus>(reader.GetString(3)), reader.GetInt64(4), reader.GetInt64(5), reader.GetInt64(6), DateTimeOffset.Parse(reader.GetString(7)), DateTimeOffset.Parse(reader.GetString(8)));
}
