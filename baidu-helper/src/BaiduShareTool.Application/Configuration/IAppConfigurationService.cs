namespace BaiduShareTool.Application.Configuration;

/// <summary>应用配置读取和保存接口。</summary>
public interface IAppConfigurationService
{
    Task<AppConfiguration> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppConfiguration configuration, CancellationToken cancellationToken = default);
}
