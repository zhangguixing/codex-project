using CommunityToolkit.Mvvm.ComponentModel;
using BaiduShareTool.Application.Configuration;

namespace BaiduShareTool.App.Services;

/// <summary>供各页面共享的登录状态，配置保存后立即更新。</summary>
public sealed partial class LoginSessionState : ObservableObject
{
    private readonly IAppConfigurationService configurationService;
    [ObservableProperty] private bool isLoggedIn;
    [ObservableProperty] private string statusText = "未登录";

    public LoginSessionState(IAppConfigurationService configurationService) => this.configurationService = configurationService;

    public async Task RefreshAsync()
    {
        var configuration = await configurationService.LoadAsync();
        IsLoggedIn = !string.IsNullOrWhiteSpace(configuration.Baidu.Bduss) || !string.IsNullOrWhiteSpace(configuration.Baidu.AccessToken);
        StatusText = IsLoggedIn ? "已登录" : "未登录";
    }

    /// <summary>清除本地登录会话并立即同步所有页面状态。</summary>
    public async Task LogoutAsync()
    {
        var configuration = await configurationService.LoadAsync();
        configuration.Baidu.Cookie = string.Empty;
        configuration.Baidu.Bduss = string.Empty;
        configuration.Baidu.Stoken = string.Empty;
        configuration.Baidu.AccessToken = string.Empty;
        await configurationService.SaveAsync(configuration);
        IsLoggedIn = false;
        StatusText = "未登录";
    }
}
