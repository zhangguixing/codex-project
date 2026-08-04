using System.Windows;
using System.Windows.Input;
using BaiduShareTool.App.Services;
namespace BaiduShareTool.App;
public partial class BaiduDirectoryPickerWindow : Window
{
    private readonly BaiduWebDirectoryService directoryService; private string currentPath = "/";
    public BaiduDirectoryPickerWindow(BaiduWebDirectoryService directoryService) { this.directoryService = directoryService; InitializeComponent(); DirectoryList.SelectionChanged += DirectoryList_SelectionChanged; Loaded += async (_, _) => await LoadAsync(); }
    public string? SelectedPath { get; private set; }
    private async Task LoadAsync()
    {
        try
        {
            IsEnabled = false; EmptyText.Visibility = Visibility.Collapsed; StatusText.Text = "正在读取目录..."; PathText.Text = currentPath;
            var directories = await directoryService.ListAsync(currentPath); DirectoryList.ItemsSource = directories; DirectoryList.SelectedItems.Clear(); SelectButton.IsEnabled = false;
            EmptyText.Visibility = directories.Count == 0 ? Visibility.Visible : Visibility.Collapsed; StatusText.Text = directories.Count == 0 ? "没有子文件夹" : $"共 {directories.Count} 个文件夹，双击进入下级";
        }
        catch (Exception exception) { StatusText.Text = exception.Message; }
        finally { IsEnabled = true; }
    }
    private async void DirectoryList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DirectoryList.SelectedItem is BaiduDirectoryEntry entry)
        {
            currentPath = entry.Path;
            await LoadAsync();
        }
    }
    private void DirectoryList_SelectionChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e) => SelectButton.IsEnabled = DirectoryList.SelectedItems.Count > 0;
    private async void Up_Click(object sender, RoutedEventArgs e) { var index = currentPath.LastIndexOf('/'); currentPath = index <= 0 ? "/" : currentPath[..index]; await LoadAsync(); }
    private void Select_Click(object sender, RoutedEventArgs e)
    {
        var selected = DirectoryList.SelectedItems.OfType<BaiduDirectoryEntry>().Select(item => item.Path).ToArray();
        if (selected.Length == 0 || selected.Any(item => item == "/")) return;
        SelectedPath = string.Join("|", selected); DialogResult = true;
    }
}
