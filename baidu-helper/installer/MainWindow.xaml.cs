using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

namespace BaiduShareTool.Installer;

public partial class MainWindow : Window
{
    private const string ApplicationName = "\u767e\u5ea6\u7f51\u76d8\u5206\u4eab\u94fe\u63a5\u52a9\u624b";

    public MainWindow()
    {
        InitializeComponent();
        InstallDirectoryTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", ApplicationName);
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { InitialDirectory = InstallDirectoryTextBox.Text };
        if (dialog.ShowDialog() == true) InstallDirectoryTextBox.Text = dialog.FolderName;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        var installDirectory = InstallDirectoryTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            StatusTextBlock.Text = "\u8bf7\u9009\u62e9\u5b89\u88c5\u76ee\u5f55\u3002";
            return;
        }

        SetInstallingState(true);
        var archivePath = Path.Combine(Path.GetTempPath(), $"BaiduShareTool-{Guid.NewGuid():N}.zip");
        try
        {
            Directory.CreateDirectory(installDirectory);
            StatusTextBlock.Text = "\u6b63\u5728\u51c6\u5907\u5b89\u88c5\u6587\u4ef6...";
            await WriteEmbeddedArchiveAsync(archivePath);
            await Task.Run(() => ExtractArchive(archivePath, installDirectory, ReportProgress));

            var executablePath = Path.Combine(installDirectory, "BaiduShareTool.App.exe");
            CreateDesktopShortcut(executablePath);
            RegisterUninstaller(installDirectory, executablePath);
            InstallProgressBar.Value = 100;
            StatusTextBlock.Text = "\u5b89\u88c5\u5b8c\u6210\uff0c\u6b63\u5728\u542f\u52a8\u7a0b\u5e8f...";
            MessageBox.Show(this, "\u5b89\u88c5\u5df2\u5b8c\u6210\uff0c\u5df2\u521b\u5efa\u684c\u9762\u5feb\u6377\u65b9\u5f0f\u3002", ApplicationName, MessageBoxButton.OK, MessageBoxImage.Information);
            Process.Start(new ProcessStartInfo(executablePath) { UseShellExecute = true });
            Close();
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = $"\u5b89\u88c5\u5931\u8d25\uff1a{exception.Message}";
            SetInstallingState(false);
        }
        finally
        {
            File.Delete(archivePath);
        }
    }

    private void ReportProgress(int completed, int total) => Dispatcher.Invoke(() =>
    {
        InstallProgressBar.Value = total == 0 ? 0 : completed * 100d / total;
        StatusTextBlock.Text = $"\u6b63\u5728\u89e3\u538b\u5b89\u88c5\u6587\u4ef6\uff1a{completed}/{total}";
    });

    private void SetInstallingState(bool isInstalling)
    {
        InstallButton.IsEnabled = !isInstalling;
        CancelButton.IsEnabled = !isInstalling;
        InstallDirectoryTextBox.IsEnabled = !isInstalling;
    }

    private static async Task WriteEmbeddedArchiveAsync(string archivePath)
    {
        await using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("BaiduShareTool.zip")
            ?? throw new InvalidOperationException("\u5b89\u88c5\u5305\u8d44\u6e90\u7f3a\u5931\u3002");
        await using var archive = File.Create(archivePath);
        await resource.CopyToAsync(archive);
    }

    private static void ExtractArchive(string archivePath, string installDirectory, Action<int, int> reportProgress)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var files = archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)).ToArray();
        var rootDirectory = Path.GetFullPath(installDirectory) + Path.DirectorySeparatorChar;

        for (var index = 0; index < files.Length; index++)
        {
            var entry = files[index];
            var destination = Path.GetFullPath(Path.Combine(installDirectory, entry.FullName));
            if (!destination.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("\u5b89\u88c5\u5305\u5305\u542b\u65e0\u6548\u8def\u5f84\u3002");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, overwrite: true);
            reportProgress(index + 1, files.Length);
        }
    }

    private static void CreateDesktopShortcut(string executablePath)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var shortcutPath = Path.Combine(desktopPath, $"{ApplicationName}.lnk");
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null) return;

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = executablePath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(executablePath);
        shortcut.Description = ApplicationName;
        shortcut.Save();
    }

    private static void RegisterUninstaller(string installDirectory, string executablePath)
    {
        var uninstallerPath = Path.Combine(installDirectory, "uninstall.exe");
        File.Copy(Environment.ProcessPath ?? throw new InvalidOperationException("无法确定安装程序路径。"), uninstallerPath, overwrite: true);

        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\BaiduShareTool");
        key.SetValue("DisplayName", ApplicationName);
        key.SetValue("DisplayVersion", "1.0.0");
        key.SetValue("Publisher", ApplicationName);
        key.SetValue("InstallLocation", installDirectory);
        key.SetValue("DisplayIcon", executablePath);
        key.SetValue("UninstallString", $"\"{uninstallerPath}\" --uninstall");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }
}
