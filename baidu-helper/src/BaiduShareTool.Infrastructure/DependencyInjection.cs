using BaiduShareTool.Application.Abstractions;
using BaiduShareTool.Application.Configuration;
using BaiduShareTool.Application.Scanning;
using BaiduShareTool.Application.Sharing;
using BaiduShareTool.Application.Caching;
using BaiduShareTool.Infrastructure.Caching;
using BaiduShareTool.Application.Tasks;
using BaiduShareTool.Infrastructure.Tasks;
using BaiduShareTool.Application.History;
using BaiduShareTool.Infrastructure.History;
using BaiduShareTool.Infrastructure.Configuration;
using BaiduShareTool.Infrastructure.Providers;
using BaiduShareTool.Providers.Abstractions;
using BaiduShareTool.Export;
using BaiduShareTool.Infrastructure.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace BaiduShareTool.Infrastructure;

/// <summary>基础设施层依赖注入注册。</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppStartupService, AppStartupService>();
        services.AddSingleton<IAppConfigurationService, AppConfigurationService>();
        services.AddSingleton<IStorageProviderRegistry, StorageProviderRegistry>();
        services.AddSingleton<IFileScanService, FileScanService>();
        services.AddSingleton<IShareLinkService, ShareLinkService>();
        services.AddSingleton<TaskPauseController>();
        services.AddSingleton<IShareCache, SqliteShareCache>();
        services.AddSingleton<ITaskCheckpointService, SqliteTaskCheckpointService>();
        services.AddSingleton<IShareTaskOrchestrator, ShareTaskOrchestrator>();
        services.AddSingleton<IHistoryService, SqliteHistoryService>();
        services.AddSingleton<IExcelExportService, OpenXmlExcelExportService>();
        return services;
    }
}
