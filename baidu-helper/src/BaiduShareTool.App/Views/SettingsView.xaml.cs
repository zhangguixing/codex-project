using System.Windows;
using System.Windows.Controls;
using BaiduShareTool.App.ViewModels;
using Microsoft.Win32;
namespace BaiduShareTool.App.Views;
public partial class SettingsView : UserControl
{
    public SettingsView() { InitializeComponent(); }

    private void BrowseExportDirectory_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsPageViewModel viewModel) return;
        var dialog = new OpenFolderDialog { Title = "选择默认导出目录", Multiselect = false };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) viewModel.ExportDirectory = dialog.FolderName;
    }

    private async void LoginWithBrowser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new BaiduBrowserLoginWindow { Owner = Window.GetWindow(this) };
            window.ShowDialog();
            if (window.LoginResult is { } result && DataContext is SettingsPageViewModel viewModel)
            {
                await viewModel.ApplyBrowserLoginAsync(result.Cookie, result.Bduss, result.Stoken);
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), $"网页登录失败：{exception.Message}", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
