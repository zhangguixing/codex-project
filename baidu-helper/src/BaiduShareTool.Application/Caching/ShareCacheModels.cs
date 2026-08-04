using BaiduShareTool.Providers.Abstractions;

namespace BaiduShareTool.Application.Caching;

/// <summary>分享缓存记录。</summary>
public sealed record ShareCacheEntry(
    string ProviderId,
    string FileId,
    string FilePath,
    string FileName,
    string? ShareUrl,
    DateTimeOffset? ShareTime,
    string Status,
    string? LastError = null,
    string PasswordFingerprint = "",
    bool PasswordVerified = false);

/// <summary>分享缓存读写接口。</summary>
public interface IShareCache
{
    Task<ShareCacheEntry?> FindAsync(string providerId, string fileId, CancellationToken cancellationToken = default);
    Task SaveAsync(ShareCacheEntry entry, CancellationToken cancellationToken = default);
}
