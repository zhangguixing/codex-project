namespace BaiduShareTool.Application.Abstractions;

/// <summary>应用启动初始化服务。</summary>
public interface IAppStartupService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
