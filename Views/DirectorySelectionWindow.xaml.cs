using System.Windows;
using System.Windows.Input;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.ViewModels;

namespace OutlastTrialsMod.Views;

public partial class DirectorySelectionWindow : Window
{
    public DirectorySelectionViewModel ViewModel { get; }

    public DirectorySelectionWindow()
    {
        ViewModel = new DirectorySelectionViewModel();
        DataContext = ViewModel;
        InitializeComponent();
        ViewModel.DirectoryConfirmed += (_, _) => DialogResult = true;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        WindowChromeHelper.OnTitleBarMouseLeftButtonDown(this, e);

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.Minimize(this);

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.CloseAndShutdown(this);
}
