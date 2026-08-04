using BaiduShareTool.Providers.Abstractions;

namespace BaiduShareTool.Application.Scanning;

/// <summary>统一执行 Provider 文件枚举和业务过滤。</summary>
public sealed class FileScanService(IStorageProviderRegistry providerRegistry) : IFileScanService
{
    public async IAsyncEnumerable<RemoteFile> ScanAsync(
        ScanRequest request,
        IProgress<ScanProgress>? progress = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var provider = providerRegistry.GetRequired(request.ProviderId);
        var discovered = 0L;
        var accepted = 0L;
        var providerOptions = new ScanOptions(request.Filter.IgnoredExtensions, request.Filter.IgnoredFolders, request.Filter.MinimumFileSize, request.Filter.MaximumFileSize);

        var roots = request.RootPath.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var root in roots)
        await foreach (var file in provider.EnumerateFilesAsync(root, providerOptions, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            discovered++;
            var acceptedFile = IsAccepted(file, request.Filter);
            if (acceptedFile)
            {
                accepted++;
                progress?.Report(new ScanProgress(discovered, accepted, GetDirectory(file.Path), file.Name));
                yield return file;
            }
            else
            {
                progress?.Report(new ScanProgress(discovered, accepted, GetDirectory(file.Path), file.Name));
            }
        }
    }

    private static bool IsAccepted(RemoteFile file, ScanFilter filter)
    {
        if (filter.MinimumFileSize > 0 && file.Size < filter.MinimumFileSize) return false;
        if (filter.MaximumFileSize > 0 && file.Size > filter.MaximumFileSize) return false;
        var extension = Path.GetExtension(file.Name);
        if (filter.IgnoredExtensions?.Contains(extension, StringComparer.OrdinalIgnoreCase) == true) return false;
        var folder = GetDirectory(file.Path);
        return filter.IgnoredFolders?.All(item => !folder.Contains(item, StringComparison.OrdinalIgnoreCase)) != false;
    }

    private static string GetDirectory(string path)
    {
        var separator = path.LastIndexOf('/');
        return separator <= 0 ? "/" : path[..separator];
    }
}
