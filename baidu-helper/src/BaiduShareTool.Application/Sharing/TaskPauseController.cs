namespace BaiduShareTool.Application.Sharing;

/// <summary>任务暂停闸门，暂停时工作线程在下一个文件边界等待。</summary>
public sealed class TaskPauseController
{
    private TaskCompletionSource<bool> resume = Completed();
    private CancellationTokenSource taskCancellation = new();

    public bool IsPaused { get; private set; }

    public CancellationToken BeginTask()
    {
        taskCancellation.Dispose();
        taskCancellation = new CancellationTokenSource();
        return taskCancellation.Token;
    }

    public void Pause() { IsPaused = true; resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); }
    public void Resume() { IsPaused = false; resume.TrySetResult(true); }
    public void Discard() { Resume(); taskCancellation.Cancel(); }
    public Task WaitIfPausedAsync(CancellationToken cancellationToken) => IsPaused ? resume.Task.WaitAsync(cancellationToken) : Task.CompletedTask;
    private static TaskCompletionSource<bool> Completed() { var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); source.SetResult(true); return source; }
}
