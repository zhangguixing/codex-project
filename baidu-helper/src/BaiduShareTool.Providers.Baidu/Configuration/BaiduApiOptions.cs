namespace BaiduShareTool.Providers.Baidu.Configuration;

/// <summary>百度开放平台参数，全部由外部配置注入，不写死账号信息。</summary>
public sealed class BaiduApiOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public Uri? AuthorizationEndpoint { get; set; }
    public Uri? TokenEndpoint { get; set; }
    public Uri? ApiBaseAddress { get; set; }
}

public sealed record BaiduOAuthToken(string AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAt);
