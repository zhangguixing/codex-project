using BaiduShareTool.Providers.Abstractions;

namespace BaiduShareTool.Application.Scanning;

/// <summary>扫描请求。</summary>
public sealed record ScanRequest(string ProviderId, string RootPath, ScanFilter Filter);

/// <summary>扫描过滤条件。</summary>
public sealed record ScanFilter(
    IReadOnlySet<string>? IgnoredFolders = null,
    IReadOnlySet<string>? IgnoredExtensions = null,
    long MinimumFileSize = 0,
    long MaximumFileSize = 0)
{
    public static ScanFilter Empty { get; } = new();
}

/// <summary>扫描进度。</summary>
public sealed record ScanProgress(
    long DiscoveredCount,
    long AcceptedCount,
    string? CurrentPath,
    string? CurrentFile);

/// <summary>扫描服务接口。</summary>
public interface IFileScanService
{
    IAsyncEnumerable<RemoteFile> ScanAsync(
        ScanRequest request,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
