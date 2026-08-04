using CommunityToolkit.Mvvm.ComponentModel;
namespace BaiduShareTool.App.Services;
public sealed partial class TaskDirectorySelectionState : ObservableObject
{
    [ObservableProperty] private string selectedPath = string.Empty;
    public string DisplayPath => string.IsNullOrWhiteSpace(SelectedPath) ? "尚未选择目录，请点击右侧“浏览目录”" : SelectedPath.Replace("|", Environment.NewLine);
    partial void OnSelectedPathChanged(string value) => OnPropertyChanged(nameof(DisplayPath));
}
