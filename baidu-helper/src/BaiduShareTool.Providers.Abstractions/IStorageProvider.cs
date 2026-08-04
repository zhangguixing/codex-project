namespace BaiduShareTool.Providers.Abstractions;

/// <summary>所有网盘提供商必须实现的最小抽象。</summary>
public interface IStorageProvider
{
    string ProviderId { get; }

    Task<AuthResult> ValidateAuthenticationAsync(
        CancellationToken cancellationToken = default);

    Task<RemoteDirectory?> GetDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<RemoteFile> EnumerateFilesAsync(
        string path,
        ScanOptions options,
        CancellationToken cancellationToken = default);

    Task<ShareResult> CreateShareLinkAsync(
        RemoteFile file,
        ShareOptions options,
        CancellationToken cancellationToken = default);
}
