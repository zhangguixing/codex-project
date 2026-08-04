using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace BaiduShareTool.App;

/// <summary>承载用户主动登录的百度网盘网页，并提取当前会话 Cookie。</summary>
public partial class BaiduBrowserLoginWindow : Window
{
    private bool sessionCaptured;
    public BaiduBrowserLoginWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            try
            {
                await Browser.EnsureCoreWebView2Async();
                Browser.NavigationCompleted += Browser_NavigationCompleted;
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, $"网页登录组件初始化失败：{exception.Message}\n请安装 Microsoft Edge WebView2 Runtime 后重试。", Title, MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        };
    }

    public BaiduBrowserLoginResult? LoginResult { get; private set; }

    private async void UseCurrentSession_Click(object sender, RoutedEventArgs e)
    {
        if (Browser.CoreWebView2 is null)
        {
            MessageBox.Show(this, "浏览器正在初始化，请稍后再试。", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        await TryCaptureSessionAsync(showMissingMessage: true);
    }

    private async void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess) await TryCaptureSessionAsync(showMissingMessage: false);
    }

    private async Task TryCaptureSessionAsync(bool showMissingMessage)
    {
        if (sessionCaptured || Browser.CoreWebView2 is null) return;
        var cookies = await Browser.CoreWebView2.CookieManager.GetCookiesAsync("https://pan.baidu.com/");
        var bduss = cookies.FirstOrDefault(item => string.Equals(item.Name, "BDUSS", StringComparison.OrdinalIgnoreCase))?.Value;
        var stoken = cookies.FirstOrDefault(item => string.Equals(item.Name, "STOKEN", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(bduss))
        {
            if (showMissingMessage) MessageBox.Show(this, "尚未检测到 BDUSS。请先完成登录，必要时进入网盘主页后再重试。", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var cookie = string.Join("; ", cookies.Select(item => $"{item.Name}={item.Value}"));
        sessionCaptured = true;
        LoginResult = new BaiduBrowserLoginResult(cookie, bduss, stoken);
        // 导航完成事件可能早于 ShowDialog 完成模态初始化，直接关闭窗口可避免 DialogResult 异常。
        Close();
    }
}

/// <summary>网页登录后返回的会话信息。</summary>
public sealed record BaiduBrowserLoginResult(string Cookie, string Bduss, string Stoken);
