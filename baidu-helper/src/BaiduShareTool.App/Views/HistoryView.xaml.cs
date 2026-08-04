using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;
using System.IO;
using BaiduShareTool.App.ViewModels;
namespace BaiduShareTool.App.Views;
public partial class HistoryView : UserControl
{
    private async void DeleteHistory_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not BaiduShareTool.Application.History.ExportHistoryRecord record || DataContext is not HistoryPageViewModel vm) return;
        if (MessageBox.Show(Window.GetWindow(this), "确定删除这条历史记录吗？导出的 Excel 文件不会被删除。", "删除历史记录", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        await vm.DeleteAsync(record);
    }
    public HistoryView() { InitializeComponent(); }
    private async void HistoryView_Loaded(object sender, RoutedEventArgs e) { if (DataContext is HistoryPageViewModel vm) await vm.LoadAsync(); }
    private void OpenHistory_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is BaiduShareTool.Application.History.ExportHistoryRecord record && File.Exists(record.FilePath)) Process.Start(new ProcessStartInfo(record.FilePath) { UseShellExecute = true }); }
}
