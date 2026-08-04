using BaiduShareTool.Providers.Abstractions;

namespace BaiduShareTool.Application.Sharing;

/// <summary>分享执行参数。</summary>
public sealed record ShareExecutionOptions(
    int ThreadCount = 4,
    int MaxRetries = 3,
    int RequestIntervalMilliseconds = 200,
    int RandomDelayMilliseconds = 0)
{
    public int EffectiveThreadCount => Math.Clamp(ThreadCount, 1, 64);
}

/// <summary>单文件分享结果。</summary>
public sealed record ShareItemResult(RemoteFile File, ShareResult Result, int RetryCount);

/// <summary>分享进度。</summary>
public sealed record ShareProgress(long ProcessedCount, long SuccessCount, long FailedCount, long TotalCount, string? CurrentFile);

/// <summary>分享服务接口。</summary>
public interface IShareLinkService
{
    Task<IReadOnlyList<ShareItemResult>> GenerateAsync(
        string providerId,
        IReadOnlyList<RemoteFile> files,
        ShareOptions options,
        ShareExecutionOptions executionOptions,
        IProgress<ShareProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
