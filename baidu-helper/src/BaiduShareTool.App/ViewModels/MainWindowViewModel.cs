using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using BaiduShareTool.Application.Configuration;
using BaiduShareTool.App.Services;
using BaiduShareTool.Application.Tasks;
using BaiduShareTool.Application.Sharing;
using BaiduShareTool.Application.Scanning;
using Microsoft.Extensions.DependencyInjection;
using BaiduShareTool.Application.History;
using BaiduShareTool.Providers.Baidu;

namespace BaiduShareTool.App.ViewModels;

/// <summary>主窗口导航状态。</summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly SettingsPageViewModel settingsPage;
    private readonly HomePageViewModel homePage;
    private readonly ShareTaskPageViewModel taskPage;
    private readonly TransferPageViewModel transferPage;
    private readonly HistoryPageViewModel historyPage;
    private readonly SupportPageViewModel supportPage = new();
    [ObservableProperty] private object currentPage = null!;
    [ObservableProperty] private string currentTitle = "首页";
    [ObservableProperty] private string refreshMessage = string.Empty;

    public MainWindowViewModel(IAppConfigurationService configurationService, LoginSessionState loginSession)
    {
        LoginSession = loginSession;
        homePage = new HomePageViewModel(loginSession, App.Services.GetRequiredService<IHistoryService>());
        settingsPage = new SettingsPageViewModel(configurationService, loginSession);
        taskPage = new ShareTaskPageViewModel(new TaskDirectorySelectionState(), configurationService, loginSession, homePage);
        transferPage = new TransferPageViewModel(new TaskDirectorySelectionState(), loginSession);
        historyPage = new HistoryPageViewModel(App.Services.GetRequiredService<IHistoryService>());
        currentPage = homePage;
    }

    public LoginSessionState LoginSession { get; }

    [RelayCommand]
    private async Task ShowHome() { CurrentPage = homePage; CurrentTitle = "首页"; await homePage.LoadAsync(); }

    [RelayCommand] private async Task RefreshPage() { RefreshMessage = "正在刷新..."; await LoginSession.RefreshAsync(); await homePage.LoadAsync(); await historyPage.LoadAsync(); RefreshMessage = $"已刷新 {DateTime.Now:HH:mm:ss}"; await Task.Delay(1800); RefreshMessage = string.Empty; }

    [RelayCommand]
    private void ShowTask() { CurrentPage = taskPage; CurrentTitle = "分享任务"; }

    [RelayCommand]
    private void ShowHistory() { CurrentPage = historyPage; CurrentTitle = "历史记录"; }

    [RelayCommand]
    private async Task ShowSettings() { CurrentPage = settingsPage; CurrentTitle = "设置"; await settingsPage.ReloadAsync(); }

    [RelayCommand]
    private void ShowSupport() { CurrentPage = supportPage; CurrentTitle = "支持作者"; }
    [RelayCommand]
    private void ShowTransfer()
    {
        CurrentPage = transferPage;
        CurrentTitle = "批量转存";
    }
}

public sealed partial class TransferPageViewModel : ObservableObject
{
    private readonly LoginSessionState loginSession;
    private readonly TaskPauseController pauseController;
    private bool discardRequested;

    [ObservableProperty] private string shareLinks = string.Empty;
    [ObservableProperty] private bool splitIntoBatches;
    [ObservableProperty] private bool isRunning;
    [ObservableProperty] private bool canPause;
    [ObservableProperty] private bool canResume;
    [ObservableProperty] private bool canDiscard;
    [ObservableProperty] private bool isIndeterminate;
    [ObservableProperty] private double progressValue;
    [ObservableProperty] private string progressText = "0/0";
    [ObservableProperty] private string statusText = "等待填写分享链接";

    public ObservableCollection<string> Logs { get; } = [];
    public TaskDirectorySelectionState DirectoryState { get; }
    public bool IsLoggedIn => loginSession.IsLoggedIn;

    public TransferPageViewModel(TaskDirectorySelectionState directoryState, LoginSessionState loginSession)
    {
        DirectoryState = directoryState;
        this.loginSession = loginSession;
        pauseController = App.Services.GetRequiredService<TaskPauseController>();
    }

    [RelayCommand]
    private void Pause()
    {
        if (!IsRunning) return;
        pauseController.Pause();
        CanPause = false;
        CanResume = true;
        StatusText = "将在当前批次完成后暂停";
    }

    [RelayCommand]
    private void Resume()
    {
        if (!IsRunning) return;
        pauseController.Resume();
        CanPause = true;
        CanResume = false;
        StatusText = "正在继续转存";
    }

    [RelayCommand]
    private void Discard()
    {
        if (!IsRunning || !CanDiscard) return;
        discardRequested = true;
        pauseController.Discard();
        CanDiscard = false;
        CanResume = false;
        StatusText = "正在停止转存";
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning) return;
        if (!loginSession.IsLoggedIn) { StatusText = "请先登录百度网盘"; return; }
        var links = ShareLinks.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (links.Length == 0) { StatusText = "请输入百度网盘分享链接"; return; }
        if (string.IsNullOrWhiteSpace(DirectoryState.SelectedPath)) { StatusText = "请选择转存目标目录"; return; }

        IsRunning = true;
        CanPause = true;
        CanResume = false;
        CanDiscard = false;
        discardRequested = false;
        IsIndeterminate = true;
        ProgressValue = 0;
        ProgressText = "正在读取分享内容";
        StatusText = "正在验证分享链接";
        Logs.Clear();
        Logs.Add($"{DateTime.Now:HH:mm:ss} 共 {links.Length} 条分享链接，目标目录：{DirectoryState.SelectedPath}");
        try
        {
            var completedLinks = 0;
            var totalTransferred = 0;
            var progress = new Progress<BaiduShareTransferProgress>(value =>
            {
                IsIndeterminate = false;
                ProgressValue = value.TotalCount == 0 ? 100 : value.TransferredCount * 100d / value.TotalCount;
                ProgressText = $"链接 {completedLinks + 1}/{links.Length}：{value.TransferredCount}/{value.TotalCount}";
                StatusText = $"正在转存链接 {completedLinks + 1}/{links.Length}";
                Logs.Add($"{DateTime.Now:HH:mm:ss} 链接 {completedLinks + 1} 的第 {value.CurrentBatch}/{value.TotalBatches} 批已完成");
            });
            var service = App.Services.GetRequiredService<BaiduWebApiClient>();
            var transferToken = pauseController.BeginTask();
            foreach (var link in links)
            {
                var result = await service.TransferSharedFilesAsync(new BaiduShareTransferRequest(link, null, DirectoryState.SelectedPath, SplitIntoBatches), progress, pauseController.WaitIfPausedAsync, transferToken);
                totalTransferred += result.TransferredCount;
                completedLinks++;
            }
            IsIndeterminate = false;
            ProgressValue = 100;
            ProgressText = $"已完成 {completedLinks}/{links.Length} 条链接，共转存 {totalTransferred} 项";
            StatusText = "转存完成";
            Logs.Add($"{DateTime.Now:HH:mm:ss} 转存完成");
        }
        catch (OperationCanceledException)
        {
            if (!discardRequested) StatusText = "转存已取消";
        }
        catch (BaiduAccountException exception)
        {
            IsIndeterminate = false;
            StatusText = exception.Message;
            Logs.Add($"{DateTime.Now:HH:mm:ss} {exception.Message}");
        }
        catch (Exception exception)
        {
            IsIndeterminate = false;
            StatusText = $"转存失败：{exception.Message}";
            Logs.Add($"{DateTime.Now:HH:mm:ss} {StatusText}");
        }
        finally
        {
            IsRunning = false;
            CanPause = false;
            CanResume = false;
            CanDiscard = false;
        }
    }

    partial void OnCanResumeChanged(bool value) => CanDiscard = value;
}

public sealed class SupportPageViewModel : ObservableObject;

public sealed partial class HistoryPageViewModel(IHistoryService historyService) : ObservableObject
{
    [ObservableProperty] private string refreshMessage = string.Empty;
    public ObservableCollection<ExportHistoryRecord> Records { get; } = [];
    public async Task LoadAsync() { RefreshMessage = "正在刷新历史记录..."; Records.Clear(); foreach (var record in await historyService.GetAllAsync()) Records.Add(record); RefreshMessage = $"已刷新 {DateTime.Now:HH:mm:ss}"; await Task.Delay(1600); RefreshMessage = string.Empty; }
    public async Task DeleteAsync(ExportHistoryRecord record) { await historyService.DeleteAsync(record.Id); Records.Remove(record); }
}

public sealed partial class HomePageViewModel : ObservableObject
{
    private readonly IHistoryService historyService;
    [ObservableProperty] private int todayGenerated;
    [ObservableProperty] private int historyTaskCount;
    public ObservableCollection<ExportHistoryRecord> RecentActivities { get; } = [];
    public HomePageViewModel(LoginSessionState loginSession, IHistoryService historyService)
    {
        this.historyService = historyService;
        LoginSession = loginSession;
        RefreshLoginStatusCommand = new AsyncRelayCommand(loginSession.RefreshAsync);
        LogoutCommand = new AsyncRelayCommand(loginSession.LogoutAsync);
    }

    public LoginSessionState LoginSession { get; }
    public IAsyncRelayCommand RefreshLoginStatusCommand { get; }
    public IAsyncRelayCommand LogoutCommand { get; }
    public async Task LoadAsync()
    {
        var records = await historyService.GetAllAsync();
        HistoryTaskCount = records.Count;
        // 数据库存储 UTC 时间，统计时转换到本机日期，避免跨午夜时归到前一天。
        var today = DateTimeOffset.Now.Date;
        TodayGenerated = records.Where(item => item.ExportTime.ToLocalTime().Date == today).Sum(item => item.SuccessCount);
        RecentActivities.Clear();
        foreach (var record in records.Take(5)) RecentActivities.Add(record);
    }
}
public sealed partial class ShareTaskPageViewModel : ObservableObject
{
    private bool discardRequested;
    private readonly IShareTaskOrchestrator orchestrator;
    private readonly IAppConfigurationService configurationService;
    private readonly LoginSessionState loginSession;
    private readonly TaskPauseController pauseController;
    private readonly HomePageViewModel homePage;
    [ObservableProperty] private string statusText = "尚未开始";
    [ObservableProperty] private bool isRunning;
    [ObservableProperty] private double progressValue;
    [ObservableProperty] private bool isIndeterminate;
    [ObservableProperty] private bool canPause;
    [ObservableProperty] private bool canResume;
    [ObservableProperty] private bool canDiscard;
    [ObservableProperty] private string progressText = "0%";
    [ObservableProperty] private string transferShareUrl = string.Empty;
    [ObservableProperty] private string transferPassword = string.Empty;
    [ObservableProperty] private string transferMode = "每批 500 个";
    public ObservableCollection<string> Logs { get; } = [];
    public TaskDirectorySelectionState DirectoryState { get; }
    public bool IsLoggedIn => loginSession.IsLoggedIn;
    public IReadOnlyList<string> TransferModes { get; } = ["每批 500 个", "全部（自动每批 500 个）"];

    public ShareTaskPageViewModel(TaskDirectorySelectionState directoryState, IAppConfigurationService configurationService, LoginSessionState loginSession, HomePageViewModel homePage)
    {
        DirectoryState = directoryState;
        this.configurationService = configurationService;
        this.loginSession = loginSession;
        this.homePage = homePage;
        orchestrator = App.Services.GetRequiredService<IShareTaskOrchestrator>();
        pauseController = App.Services.GetRequiredService<TaskPauseController>();
    }

    [RelayCommand] private void Pause() { if (!IsRunning) return; pauseController.Pause(); CanPause = false; CanResume = true; StatusText = "已暂停，当前文件完成后等待继续"; }
    [RelayCommand] private void Resume() { if (!IsRunning) return; pauseController.Resume(); CanPause = true; CanResume = false; StatusText = "继续处理中"; }

    partial void OnCanResumeChanged(bool value) => CanDiscard = value;
    partial void OnIsRunningChanged(bool value) { if (value) discardRequested = false; }

    [RelayCommand]
    private void Discard()
    {
        if (!IsRunning || !CanDiscard) return;
        discardRequested = true;
        IsIndeterminate = false;
        ProgressValue = 0;
        ProgressText = "0%";
        Logs.Clear();
        CanDiscard = false;
        CanResume = false;
        pauseController.Discard();
        StatusText = "正在丢弃任务...";
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning) return;
        if (string.IsNullOrWhiteSpace(DirectoryState.SelectedPath)) { StatusText = "请先选择百度网盘目录"; return; }
        if (!loginSession.IsLoggedIn) { StatusText = "请先登录百度网盘"; return; }
        IsRunning = true; CanPause = true; CanResume = false; IsIndeterminate = true; ProgressValue = 0; Logs.Clear(); StatusText = "正在扫描目录..."; Logs.Add($"{DateTime.Now:HH:mm:ss} 开始扫描：{DirectoryState.SelectedPath}");
        try
        {
            var configuration = await configurationService.LoadAsync();
            var sharePassword = configuration.Baidu.DefaultSharePassword.Trim();
            if (!IsValidSharePassword(sharePassword)) throw new InvalidOperationException("默认分享密码必须为 4 位英文字母或数字，请在设置中修改并保存");
            var directoryName = DirectoryState.SelectedPath.Trim('/').Split('/').LastOrDefault() ?? "根目录";
            var outputPath = Path.Combine(configuration.Export.Directory, directoryName + ".xlsx");
            var scanProgress = new Progress<ScanProgress>(value => { StatusText = $"扫描中：{value.CurrentFile}"; Logs.Add($"{DateTime.Now:HH:mm:ss} 发现文件：{value.CurrentFile}"); });
            var shareProgress = new Progress<ShareProgress>(value => { IsIndeterminate = false; ProgressValue = value.TotalCount == 0 ? 100 : value.ProcessedCount * 100d / value.TotalCount; ProgressText = $"{value.ProcessedCount}/{value.TotalCount}，成功 {value.SuccessCount}，失败 {value.FailedCount}"; StatusText = value.TotalCount == 0 ? "没有可处理的文件" : $"生成中：{value.CurrentFile}"; if (!string.IsNullOrWhiteSpace(value.CurrentFile)) Logs.Add($"{DateTime.Now:HH:mm:ss} {value.CurrentFile} {(value.FailedCount > 0 ? "处理失败" : "生成成功")}"); });
            Logs.Add($"{DateTime.Now:HH:mm:ss} 已确认 4 位默认分享密码，将创建受密码保护的链接");
            var result = await orchestrator.StartAsync(new ShareTaskRequest("baidu", DirectoryState.SelectedPath, ScanFilter.Empty, new BaiduShareTool.Providers.Abstractions.ShareOptions(sharePassword, 0), new ShareExecutionOptions(configuration.Network.ThreadCount, configuration.Network.MaxRetries, configuration.Network.RequestIntervalMilliseconds), outputPath), scanProgress, shareProgress);
            await homePage.LoadAsync();
            IsIndeterminate = false; ProgressValue = 100; ProgressText = $"完成：成功 {result.SuccessCount}，失败 {result.FailedCount}"; StatusText = ProgressText; Logs.Add($"{DateTime.Now:HH:mm:ss} 写入 Excel：{outputPath}");
            if (configuration.Export.AutoOpen && File.Exists(outputPath))
            {
                try
                {
                    var launched = Process.Start(new ProcessStartInfo { FileName = outputPath, UseShellExecute = true });
                    if (launched is null) Process.Start(new ProcessStartInfo("explorer.exe", $"\"{outputPath}\"") { UseShellExecute = true });
                    Logs.Add($"{DateTime.Now:HH:mm:ss} 已自动打开 Excel 文件");
                }
                catch (Exception openException)
                {
                    Logs.Add($"{DateTime.Now:HH:mm:ss} 自动打开失败：{openException.Message}");
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{outputPath}\"") { UseShellExecute = true });
                }
            }
        }
        catch (OperationCanceledException) { if (!discardRequested) StatusText = "任务已取消"; }
        catch (Exception exception) { StatusText = $"任务失败：{exception.Message}"; }
        finally { IsRunning = false; CanPause = false; CanResume = false; CanDiscard = false; }
    }

    [RelayCommand]
    private async Task StartTransferAsync()
    {
        if (IsRunning) return;
        if (!loginSession.IsLoggedIn) { StatusText = "请先登录百度网盘"; return; }
        if (string.IsNullOrWhiteSpace(TransferShareUrl)) { StatusText = "请输入百度网盘分享链接"; return; }
        if (string.IsNullOrWhiteSpace(DirectoryState.SelectedPath)) { StatusText = "请先选择转存目标目录"; return; }

        IsRunning = true;
        CanPause = true;
        CanResume = false;
        IsIndeterminate = true;
        ProgressValue = 0;
        ProgressText = "准备转存";
        Logs.Clear();
        try
        {
            var transferAll = TransferMode.StartsWith("全部", StringComparison.Ordinal);
            var progress = new Progress<BaiduShareTransferProgress>(value =>
            {
                IsIndeterminate = false;
                ProgressValue = value.TotalCount == 0 ? 100 : value.TransferredCount * 100d / value.TotalCount;
                ProgressText = $"{value.TransferredCount}/{value.TotalCount}，第 {value.CurrentBatch}/{value.TotalBatches} 批";
                StatusText = $"正在转存第 {value.CurrentBatch}/{value.TotalBatches} 批";
                Logs.Add($"{DateTime.Now:HH:mm:ss} 已完成第 {value.CurrentBatch} 批，共 {value.TransferredCount} 个文件");
            });
            Logs.Add($"{DateTime.Now:HH:mm:ss} 开始转存到：{DirectoryState.SelectedPath}");
            var service = App.Services.GetRequiredService<BaiduWebApiClient>();
            var result = await service.TransferSharedFilesAsync(new BaiduShareTransferRequest(TransferShareUrl, TransferPassword, DirectoryState.SelectedPath, transferAll), progress, pauseController.WaitIfPausedAsync, pauseController.BeginTask());
            IsIndeterminate = false;
            ProgressValue = result.AvailableCount == 0 ? 100 : result.TransferredCount * 100d / result.AvailableCount;
            ProgressText = $"完成：已转存 {result.TransferredCount}/{result.AvailableCount}，共 {result.BatchCount} 批";
            StatusText = ProgressText;
            Logs.Add($"{DateTime.Now:HH:mm:ss} 转存完成");
        }
        catch (OperationCanceledException) { if (!discardRequested) StatusText = "转存已取消"; }
        catch (Exception exception) { StatusText = $"转存失败：{exception.Message}"; Logs.Add($"{DateTime.Now:HH:mm:ss} {StatusText}"); }
        finally { IsRunning = false; CanPause = false; CanResume = false; CanDiscard = false; }
    }

    private static bool IsValidSharePassword(string value) => value.Length == 4 && value.All(char.IsLetterOrDigit);
}
public sealed partial class SettingsPageViewModel : ObservableObject
{
    private readonly IAppConfigurationService configurationService;
    private readonly LoginSessionState loginSession;
    private AppConfiguration configuration;

    [ObservableProperty] private string cookie = string.Empty;
    [ObservableProperty] private string bduss = string.Empty;
    [ObservableProperty] private string sharePassword = string.Empty;
    [ObservableProperty] private string exportDirectory = string.Empty;
    [ObservableProperty] private string exportFileName = "share.xlsx";
    [ObservableProperty] private bool autoOpen = true;
    [ObservableProperty] private string saveMessage = string.Empty;

    public SettingsPageViewModel(IAppConfigurationService configurationService, LoginSessionState loginSession)
    {
        this.configurationService = configurationService;
        this.loginSession = loginSession;
        configuration = configurationService.LoadAsync().GetAwaiter().GetResult();
        Cookie = configuration.Baidu.Cookie;
        Bduss = configuration.Baidu.Bduss;
        SharePassword = configuration.Baidu.DefaultSharePassword;
        ExportDirectory = configuration.Export.Directory;
        ExportFileName = configuration.Export.FileName;
        AutoOpen = configuration.Export.AutoOpen;
    }

    public async Task ReloadAsync()
    {
        configuration = await configurationService.LoadAsync();
        Cookie = configuration.Baidu.Cookie;
        Bduss = configuration.Baidu.Bduss;
        SharePassword = configuration.Baidu.DefaultSharePassword;
        ExportDirectory = configuration.Export.Directory;
        ExportFileName = configuration.Export.FileName;
        AutoOpen = configuration.Export.AutoOpen;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        SharePassword = SharePassword.Trim();
        var passwordWarning = !string.IsNullOrWhiteSpace(SharePassword) && (SharePassword.Length != 4 || !SharePassword.All(char.IsLetterOrDigit));
        configuration.Baidu.Cookie = Cookie;
        configuration.Baidu.Bduss = Bduss;
        configuration.Baidu.DefaultSharePassword = SharePassword;
        configuration.Export.Directory = ExportDirectory;
        configuration.Export.FileName = ExportFileName;
        configuration.Export.AutoOpen = AutoOpen;
        await configurationService.SaveAsync(configuration);
        await loginSession.RefreshAsync();
        SaveMessage = passwordWarning ? "配置已保存；分享密码需为 4 位字母或数字" : $"已保存 {DateTime.Now:HH:mm:ss}";
    }

    /// <summary>接收用户在嵌入浏览器完成登录后取得的会话信息。</summary>
    public async Task ApplyBrowserLoginAsync(string cookie, string bduss, string stoken)
    {
        Cookie = cookie;
        Bduss = bduss;
        configuration.Baidu.Stoken = stoken;
        await SaveAsync();
        SaveMessage = "登录会话已保存";
    }
}
