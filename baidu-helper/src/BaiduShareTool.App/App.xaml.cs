using System.IO;
using System.Windows;
using System.Windows.Threading;
using BaiduShareTool.Application.Abstractions;
using BaiduShareTool.Application.Configuration;
using BaiduShareTool.Infrastructure;
using BaiduShareTool.App.ViewModels;
using BaiduShareTool.App.Services;
using BaiduShareTool.Providers.Baidu;
using BaiduShareTool.Providers.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace BaiduShareTool.App;

/// <summary>WPF 应用入口，负责宿主生命周期和依赖注入。</summary>
public partial class App : System.Windows.Application
{
    private IHost? host;
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "data", "logs"));
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "data", "logs", "app-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddInfrastructure();
                services.AddSingleton<LoginSessionState>();
                services.AddSingleton<TaskDirectorySelectionState>();
                services.AddSingleton<BaiduWebDirectoryService>();
                services.AddBaiduProvider();
                services.AddSimulationProvider();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
        Services = host.Services;
        await host.StartAsync();
        await host.Services.GetRequiredService<IAppStartupService>().InitializeAsync();
        await host.Services.GetRequiredService<LoginSessionState>().RefreshAsync();
        var mainWindow = host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.WindowState = WindowState.Normal;
        mainWindow.Show();
        mainWindow.Activate();
        mainWindow.Topmost = true;
        mainWindow.Topmost = false;
        mainWindow.Focus();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "未处理的 UI 异常");
        MessageBox.Show($"程序发生异常：{e.Exception.Message}\n\n详细日志已写入 data/logs。", "百度网盘分享工具", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (host is not null)
        {
            await host.StopAsync();
            host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
