using System.Windows;
using System.Windows.Controls;
using BaiduShareTool.App.Services;
using BaiduShareTool.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BaiduShareTool.App.Views;

public partial class TransferView : UserControl
{
    public TransferView() => InitializeComponent();

    private async void BrowseDirectory_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TransferPageViewModel viewModel) return;
        if (!viewModel.IsLoggedIn)
        {
            MessageBox.Show(Window.GetWindow(this), "请先登录百度网盘，再选择转存目标目录。", "请先登录", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var window = new BaiduDirectoryPickerWindow(App.Services.GetRequiredService<BaiduWebDirectoryService>()) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true && window.SelectedPath is not null) viewModel.DirectoryState.SelectedPath = window.SelectedPath;
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "目录读取失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        await Task.CompletedTask;
    }

    private async void StartTransfer_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TransferPageViewModel viewModel) return;
        await viewModel.StartCommand.ExecuteAsync(null);
    }
}
