using BaiduShareTool.Application.History;
using BaiduShareTool.Application.Scanning;
using BaiduShareTool.Application.Sharing;
using BaiduShareTool.Export;
using BaiduShareTool.Providers.Abstractions;

namespace BaiduShareTool.Application.Tasks;

public sealed record ShareTaskRequest(string ProviderId, string RootPath, ScanFilter Filter, ShareOptions ShareOptions, ShareExecutionOptions ExecutionOptions, string OutputPath);
public sealed record TaskExecutionResult(string TaskId, int ScannedCount, int SuccessCount, int FailedCount, string OutputPath);

public interface IShareTaskOrchestrator
{
    Task<TaskExecutionResult> StartAsync(ShareTaskRequest request, IProgress<ScanProgress>? scanProgress = null, IProgress<ShareProgress>? shareProgress = null, CancellationToken cancellationToken = default);
}

public sealed class ShareTaskOrchestrator(
    IFileScanService scanner,
    IShareLinkService sharer,
    IExcelExportService exporter,
    ITaskCheckpointService checkpoints,
    IHistoryService historyService,
    TaskPauseController pauseController) : IShareTaskOrchestrator
{
    public async Task<TaskExecutionResult> StartAsync(ShareTaskRequest request, IProgress<ScanProgress>? scanProgress = null, IProgress<ShareProgress>? shareProgress = null, CancellationToken cancellationToken = default)
    {
        using var taskCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, pauseController.BeginTask());
        var executionToken = taskCancellation.Token;
        ShareTask? task = null;
        try
        {
            task = await checkpoints.CreateAsync(request.ProviderId, request.RootPath, executionToken);
            await checkpoints.UpdateTaskStatusAsync(task.TaskId, TaskStatus.Scanning, executionToken);

            var scannedFiles = new List<RemoteFile>();
            await foreach (var file in scanner.ScanAsync(new ScanRequest(request.ProviderId, request.RootPath, request.Filter), scanProgress, executionToken))
            {
                await checkpoints.AddItemAsync(task.TaskId, file, executionToken);
                scannedFiles.Add(file);
            }

            await checkpoints.UpdateTaskStatusAsync(task.TaskId, TaskStatus.Running, executionToken);
            var results = await sharer.GenerateAsync(request.ProviderId, scannedFiles, request.ShareOptions, request.ExecutionOptions, shareProgress, executionToken);
            foreach (var item in results)
            {
                await checkpoints.UpdateItemAsync(task.TaskId, item.File.Id, item.Result.IsSuccess ? TaskItemStatus.Success : TaskItemStatus.Failed, item.Result.Url, item.RetryCount, item.Result.ErrorMessage, executionToken);
            }

            async IAsyncEnumerable<ExportRow> ExportRows()
            {
                foreach (var item in results)
                {
                    executionToken.ThrowIfCancellationRequested();
                    yield return new ExportRow(item.File.Name, item.Result.Url ?? string.Empty);
                    await Task.Yield();
                }
            }

            await exporter.ExportAsync(ExportRows(), new ExportOptions(request.OutputPath), cancellationToken: executionToken);
            await checkpoints.UpdateTaskStatusAsync(task.TaskId, TaskStatus.Completed, executionToken);
            await historyService.AddAsync(new ExportHistoryRecord(0, task.TaskId, DateTimeOffset.UtcNow, request.RootPath, results.Count, results.Count(item => item.Result.IsSuccess), results.Count(item => !item.Result.IsSuccess), request.OutputPath), executionToken);
            return new TaskExecutionResult(task.TaskId, results.Count, results.Count(item => item.Result.IsSuccess), results.Count(item => !item.Result.IsSuccess), request.OutputPath);
        }
        catch (OperationCanceledException)
        {
            if (task is not null)
                await checkpoints.UpdateTaskStatusAsync(task.TaskId, TaskStatus.Stopped, CancellationToken.None);
            throw;
        }
    }
}
