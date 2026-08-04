using BaiduShareTool.Providers.Baidu.Configuration;

namespace BaiduShareTool.Providers.Baidu.OAuth;

/// <summary>百度 OAuth 流程抽象。</summary>
public interface IBaiduOAuthService
{
    Uri BuildAuthorizationUri(BaiduApiOptions options, string state);
    Task<BaiduOAuthToken> ExchangeCodeAsync(BaiduApiOptions options, string code, CancellationToken cancellationToken = default);
}
