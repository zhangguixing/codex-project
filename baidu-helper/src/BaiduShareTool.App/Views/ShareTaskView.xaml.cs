using System.Windows;
using System.Windows.Controls;
using BaiduShareTool.App.Services;
using BaiduShareTool.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BaiduShareTool.App.Views;

public partial class ShareTaskView : UserControl
{
    public ShareTaskView() => InitializeComponent();

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ShareTaskPageViewModel viewModel)
        {
            MessageBox.Show(Window.GetWindow(this), "\u4efb\u52a1\u9875\u9762\u5c1a\u672a\u5b8c\u6210\u521d\u59cb\u5316\uff0c\u8bf7\u91cd\u65b0\u6253\u5f00\u5206\u4eab\u4efb\u52a1\u9875\u9762\u3002", "\u4efb\u52a1\u672a\u542f\u52a8", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await viewModel.StartCommand.ExecuteAsync(null);
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "\u4efb\u52a1\u542f\u52a8\u5931\u8d25", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BrowseDirectory_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ShareTaskPageViewModel viewModel)
        {
            MessageBox.Show(Window.GetWindow(this), "\u4efb\u52a1\u9875\u9762\u5c1a\u672a\u5b8c\u6210\u521d\u59cb\u5316\u3002", "\u63d0\u793a", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!viewModel.IsLoggedIn)
        {
            MessageBox.Show(Window.GetWindow(this), "\u8bf7\u5148\u767b\u5f55\u767e\u5ea6\u7f51\u76d8\uff0c\u767b\u5f55\u540e\u518d\u6d4f\u89c8\u76ee\u5f55\u3002", "\u8bf7\u5148\u767b\u5f55", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var window = new BaiduDirectoryPickerWindow(App.Services.GetRequiredService<BaiduWebDirectoryService>()) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true && window.SelectedPath is not null) viewModel.DirectoryState.SelectedPath = window.SelectedPath;
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "\u76ee\u5f55\u8bfb\u53d6\u5931\u8d25", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        await Task.CompletedTask;
    }

    private async void Transfer_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ShareTaskPageViewModel viewModel) return;
        try
        {
            await viewModel.StartTransferCommand.ExecuteAsync(null);
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "转存启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
