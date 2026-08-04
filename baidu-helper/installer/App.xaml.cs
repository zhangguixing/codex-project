using System.Windows;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace BaiduShareTool.Installer;

public partial class App : Application
{
    private const string ApplicationName = "\u767e\u5ea6\u7f51\u76d8\u5206\u4eab\u94fe\u63a5\u52a9\u624b";
    private const string UninstallKeyName = "BaiduShareTool";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase))
        {
            Uninstall();
            Shutdown();
            return;
        }

        new MainWindow().Show();
    }

    private static void Uninstall()
    {
        var installDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        if (MessageBox.Show("确定要卸载百度网盘分享链接助手吗？", "卸载百度网盘分享链接助手", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallKeyName}", throwOnMissingSubKey: false);
        var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{ApplicationName}.lnk");
        File.Delete(shortcutPath);
        var scriptPath = Path.Combine(Path.GetTempPath(), $"BaiduShareTool-Uninstall-{Guid.NewGuid():N}.cmd");
        File.WriteAllText(scriptPath, $"@echo off\r\ntimeout /t 2 /nobreak > nul\r\nrmdir /s /q \"{installDirectory}\"\r\ndel \"%~f0\"\r\n");
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"\"{scriptPath}\"\"") { CreateNoWindow = true, UseShellExecute = false });
        MessageBox.Show("卸载已完成。", "百度网盘分享链接助手", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
