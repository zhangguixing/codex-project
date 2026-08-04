using BaiduShareTool.Providers.Abstractions;

namespace BaiduShareTool.Application.Tasks;

public enum TaskStatus { Created, Scanning, Running, Paused, Completed, Stopped, Failed }
public enum TaskItemStatus { Pending, Processing, Success, Failed, Skipped }

/// <summary>可恢复的任务快照。</summary>
public sealed record ShareTask(
    string TaskId,
    string ProviderId,
    string RootPath,
    TaskStatus Status,
    long ScannedCount,
    long SuccessCount,
    long FailedCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ShareTaskItem(
    long Id,
    string TaskId,
    RemoteFile File,
    TaskItemStatus Status,
    string? ShareUrl,
    int RetryCount,
    string? LastError,
    DateTimeOffset UpdatedAt);

public interface ITaskCheckpointService
{
    Task<ShareTask> CreateAsync(string providerId, string rootPath, CancellationToken cancellationToken = default);
    Task<ShareTask?> GetAsync(string taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShareTask>> GetResumableAsync(CancellationToken cancellationToken = default);
    Task AddItemAsync(string taskId, RemoteFile file, CancellationToken cancellationToken = default);
    Task UpdateItemAsync(string taskId, string fileId, TaskItemStatus status, string? shareUrl = null, int retryCount = 0, string? error = null, CancellationToken cancellationToken = default);
    Task UpdateTaskStatusAsync(string taskId, TaskStatus status, CancellationToken cancellationToken = default);
}
