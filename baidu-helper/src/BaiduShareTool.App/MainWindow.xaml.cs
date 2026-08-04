using System.Windows;
using System.Windows.Media.Animation;

namespace BaiduShareTool.App;

/// <summary>基础主窗口。</summary>
public partial class MainWindow : Window
{
    public MainWindow(ViewModels.MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new QuadraticEase() };
        BeginAnimation(OpacityProperty, animation);
        if (DataContext is ViewModels.MainWindowViewModel viewModel)
            _ = viewModel.RefreshPageCommand.ExecuteAsync(null);
    }
}
