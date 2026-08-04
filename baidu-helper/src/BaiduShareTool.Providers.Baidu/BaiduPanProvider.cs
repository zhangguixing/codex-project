using BaiduShareTool.Providers.Abstractions;

namespace BaiduShareTool.Providers.Baidu;

/// <summary>百度网盘官方 Provider 占位实现。</summary>
public sealed class BaiduPanProvider(BaiduWebApiClient webApiClient) : IStorageProvider
{
    public string ProviderId => "baidu";

    public Task<AuthResult> ValidateAuthenticationAsync(CancellationToken cancellationToken = default)
        => webApiClient.ValidateAsync(cancellationToken);

    public Task<RemoteDirectory?> GetDirectoryAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult<RemoteDirectory?>(new RemoteDirectory(path, path, Path.GetFileName(path.TrimEnd('/'))));

    public async IAsyncEnumerable<RemoteFile> EnumerateFilesAsync(string path, ScanOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var directories = new Stack<string>(); directories.Push(path);
        while (directories.Count > 0)
        {
            var current = directories.Pop();
            foreach (var entry in await webApiClient.ListAsync(current, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.IsDirectory) directories.Push(entry.Path);
                else yield return new RemoteFile(entry.Id, entry.Path, entry.Name, entry.Size);
            }
        }
    }

    public Task<ShareResult> CreateShareLinkAsync(RemoteFile file, ShareOptions options, CancellationToken cancellationToken = default)
        => webApiClient.CreateShareAsync(file, options, cancellationToken);
}
