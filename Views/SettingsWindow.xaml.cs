using System.Windows;
using System.Windows.Input;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.ViewModels;

namespace OutlastTrialsMod.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        DataContext = new SettingsViewModel();
        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        WindowChromeHelper.OnTitleBarMouseLeftButtonDown(this, e);

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.Minimize(this);

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.Close(this);
}
