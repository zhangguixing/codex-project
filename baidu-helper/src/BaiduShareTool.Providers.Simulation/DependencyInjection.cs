using BaiduShareTool.Providers.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BaiduShareTool.Providers.Simulation;

public static class DependencyInjection
{
    public static IServiceCollection AddSimulationProvider(this IServiceCollection services)
    {
        services.AddSingleton<IStorageProvider, InMemoryStorageProvider>();
        return services;
    }
}
