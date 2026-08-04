using BaiduShareTool.Providers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using BaiduShareTool.Providers.Baidu.Configuration;
using BaiduShareTool.Providers.Baidu.OAuth;

namespace BaiduShareTool.Providers.Baidu;

/// <summary>百度 Provider 依赖注入注册。</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBaiduProvider(this IServiceCollection services)
    {
        services.AddSingleton(new BaiduApiOptions());
        services.AddHttpClient("baidu-oauth");
        services.AddHttpClient("baidu-web");
        services.AddSingleton<BaiduWebApiClient>();
        services.AddHttpClient<BaiduApiClient>();
        services.AddSingleton<IBaiduOAuthService, BaiduOAuthService>();
        services.AddSingleton<IStorageProvider, BaiduPanProvider>();
        return services;
    }
}
