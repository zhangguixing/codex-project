using BaiduShareTool.Providers.Abstractions;

namespace BaiduShareTool.Providers.Simulation;

/// <summary>本地模拟 Provider，用于 UI 和任务编排测试，不访问网络。</summary>
public sealed class InMemoryStorageProvider : IStorageProvider
{
    private readonly IReadOnlyList<RemoteFile> files =
    [
        new("demo-001", "/示例/功夫熊猫.mp4", "功夫熊猫.mp4", 1024 * 1024 * 700),
        new("demo-002", "/示例/abc.tar.gz", "abc.tar.gz", 1024 * 20),
        new("demo-003", "/示例/哈利波特.mkv", "哈利波特.mkv", 1024 * 1024 * 1200)
    ];

    public string ProviderId => "simulation";
    public Task<AuthResult> ValidateAuthenticationAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new AuthResult(true, "模拟 Provider 已就绪"));
    public Task<RemoteDirectory?> GetDirectoryAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult<RemoteDirectory?>(new RemoteDirectory("demo-root", path, Path.GetFileName(path.TrimEnd('/'))));
    public async IAsyncEnumerable<RemoteFile> EnumerateFilesAsync(string path, ScanOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in files.Where(item => item.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return file;
        }
    }
    public Task<ShareResult> CreateShareLinkAsync(RemoteFile file, ShareOptions options, CancellationToken cancellationToken = default)
        => Task.FromResult(new ShareResult(true, $"https://example.invalid/share/{file.Id}"));
}
