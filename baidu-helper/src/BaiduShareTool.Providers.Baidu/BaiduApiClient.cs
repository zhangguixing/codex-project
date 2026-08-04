using BaiduShareTool.Providers.Abstractions;
using BaiduShareTool.Providers.Baidu.Configuration;

namespace BaiduShareTool.Providers.Baidu;

/// <summary>百度 API 传输层。未确认的接口不会被猜测实现。</summary>
public sealed class BaiduApiClient(HttpClient httpClient, BaiduApiOptions options)
{
    public Task<AuthResult> ValidateAuthenticationAsync(CancellationToken cancellationToken)
    {
        var configured = options.ApiBaseAddress is not null || httpClient.BaseAddress is not null;
        var message = configured ? "官方认证接口待根据应用权限接入" : "未配置百度官方 API 地址";
        return Task.FromResult(new AuthResult(false, message));
    }
}
