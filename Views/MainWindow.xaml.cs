using System.Windows;
using System.Windows.Input;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Services;
using OutlastTrialsMod.ViewModels;

namespace OutlastTrialsMod.Views;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        ViewModel = new MainViewModel();
        DataContext = ViewModel;
        InitializeComponent();
        Closing += (_, _) =>
        {
            ViewModel.BrowserTab.CancelPendingWork();
            ViewModel.ModTab.CancelPendingWork();
            Cue4ParseService.Instance.DisposeProvider();
        };
        Closed += (_, _) => System.Windows.Application.Current.Shutdown();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        WindowChromeHelper.OnTitleBarMouseLeftButtonDown(this, e);

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.Minimize(this);

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.ToggleMaximize(this);

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
        System.Windows.Application.Current.Shutdown();
    }
}
