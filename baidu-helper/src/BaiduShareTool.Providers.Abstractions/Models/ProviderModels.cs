namespace BaiduShareTool.Providers.Abstractions;

/// <summary>Provider 认证状态。</summary>
public sealed record AuthResult(bool IsAuthenticated, string? Message = null);

/// <summary>远程目录。</summary>
public sealed record RemoteDirectory(string Id, string Path, string Name);

/// <summary>远程文件。</summary>
public sealed record RemoteFile(string Id, string Path, string Name, long Size, DateTimeOffset? ModifiedAt = null);

/// <summary>扫描过滤选项。</summary>
public sealed record ScanOptions(
    IReadOnlySet<string>? IgnoredExtensions = null,
    IReadOnlySet<string>? IgnoredFolders = null,
    long MinimumSize = 0,
    long MaximumSize = 0);

/// <summary>分享链接选项。</summary>
public sealed record ShareOptions(string Password, int ValidDays);

/// <summary>分享结果。</summary>
public sealed record ShareResult(bool IsSuccess, string? Url = null, string? ErrorMessage = null, bool IsSupported = true);

/// <summary>Provider 未实现官方能力时使用的异常。</summary>
public sealed class ProviderNotSupportedException(string providerId, string capability)
    : NotSupportedException($"Provider '{providerId}' does not support capability '{capability}'.")
{
    public string ProviderId { get; } = providerId;
    public string Capability { get; } = capability;
}
