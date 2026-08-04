using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BaiduShareTool.Providers.Baidu.Configuration;

namespace BaiduShareTool.Providers.Baidu.OAuth;

/// <summary>仅负责标准 OAuth 参数构造和令牌交换，不假设百度私有接口。</summary>
public sealed class BaiduOAuthService(IHttpClientFactory httpClientFactory) : IBaiduOAuthService
{
    public Uri BuildAuthorizationUri(BaiduApiOptions options, string state)
    {
        if (options.AuthorizationEndpoint is null) throw new InvalidOperationException("未配置 OAuth 授权地址");
        var query = string.Join("&", new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = options.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = options.Scope,
            ["state"] = state
        }.Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
        return new UriBuilder(options.AuthorizationEndpoint) { Query = query }.Uri;
    }

    public async Task<BaiduOAuthToken> ExchangeCodeAsync(BaiduApiOptions options, string code, CancellationToken cancellationToken = default)
    {
        if (options.TokenEndpoint is null) throw new InvalidOperationException("未配置 OAuth 令牌地址");
        using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["redirect_uri"] = options.RedirectUri
            })
        };
        var response = await httpClientFactory.CreateClient("baidu-oauth").SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("OAuth 响应为空");
        if (string.IsNullOrWhiteSpace(payload.AccessToken)) throw new InvalidOperationException("OAuth 响应缺少 access_token");
        return new BaiduOAuthToken(payload.AccessToken, payload.RefreshToken, payload.ExpiresIn is null ? null : DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn.Value));
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] long? ExpiresIn);
}
