using System.Collections.Concurrent;
using System.Threading.Channels;
using BaiduShareTool.Providers.Abstractions;

namespace BaiduShareTool.Application.Sharing;

/// <summary>基于有界 Channel 的并发分享执行器。</summary>
public sealed class ShareLinkService(IStorageProviderRegistry providerRegistry, TaskPauseController pauseController) : IShareLinkService
{
    public async Task<IReadOnlyList<ShareItemResult>> GenerateAsync(
        string providerId,
        IReadOnlyList<RemoteFile> files,
        ShareOptions options,
        ShareExecutionOptions executionOptions,
        IProgress<ShareProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var provider = providerRegistry.GetRequired(providerId);
        // 版本化指纹可使旧创建方式产生的缓存自动失效并以当前网页端请求重新创建。
        var channel = Channel.CreateBounded<RemoteFile>(new BoundedChannelOptions(executionOptions.EffectiveThreadCount * 2)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true
        });
        var results = new ConcurrentBag<ShareItemResult>();
        var processed = 0L;
        var success = 0L;
        var failed = 0L;
        var total = files.Count;
        progress?.Report(new ShareProgress(0, 0, 0, total, null));

        var producer = Task.Run(async () =>
        {
            try
            {
                foreach (var file in files) { cancellationToken.ThrowIfCancellationRequested(); await channel.Writer.WriteAsync(file, cancellationToken); }
            }
            finally { channel.Writer.TryComplete(); }
        }, cancellationToken);

        var workers = Enumerable.Range(0, executionOptions.EffectiveThreadCount).Select(_ => Task.Run(async () =>
        {
            await foreach (var file in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await pauseController.WaitIfPausedAsync(cancellationToken);
                // 每次任务都重新调用 Provider，确保百度端的分享密码和有效期立即生效。
                var result = await CreateWithRetryAsync(provider, file, options, executionOptions, cancellationToken);
                results.Add(result);
                var currentProcessed = Interlocked.Increment(ref processed);
                if (result.Result.IsSuccess) Interlocked.Increment(ref success); else Interlocked.Increment(ref failed);
                progress?.Report(new ShareProgress(currentProcessed, Volatile.Read(ref success), Volatile.Read(ref failed), total, file.Name));
            }
        }, cancellationToken)).ToArray();

        await Task.WhenAll(workers.Append(producer));
        return results.OrderBy(item => item.File.Path, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static async Task<ShareItemResult> CreateWithRetryAsync(
        IStorageProvider provider,
        RemoteFile file,
        ShareOptions options,
        ShareExecutionOptions executionOptions,
        CancellationToken cancellationToken)
    {
        ShareResult result = new(false, ErrorMessage: "未执行");
        var retryCount = 0;
        for (var attempt = 0; attempt <= Math.Max(0, executionOptions.MaxRetries); attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (attempt > 0) await Task.Delay(TimeSpan.FromMilliseconds(executionOptions.RequestIntervalMilliseconds * attempt), cancellationToken);
                result = await provider.CreateShareLinkAsync(file, options, cancellationToken);
                if (result.IsSuccess || !result.IsSupported) break;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception)
            {
                result = new ShareResult(false, ErrorMessage: exception.Message);
            }
            retryCount = attempt + 1;
            if (executionOptions.RandomDelayMilliseconds > 0)
                await Task.Delay(Random.Shared.Next(0, executionOptions.RandomDelayMilliseconds + 1), cancellationToken);
        }
        return new ShareItemResult(file, result, retryCount);
    }
}
