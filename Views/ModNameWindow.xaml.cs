using System.Windows;
using System.Windows.Input;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.ViewModels;

namespace OutlastTrialsMod.Views;

public partial class ModNameWindow : Window
{
    public ModNameViewModel ViewModel { get; }

    public ModNameWindow()
    {
        ViewModel = new ModNameViewModel();
        DataContext = ViewModel;
        InitializeComponent();
        ViewModel.Confirmed += (_, _) => DialogResult = true;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        WindowChromeHelper.OnTitleBarMouseLeftButtonDown(this, e);

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.Minimize(this);

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.Close(this);

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
