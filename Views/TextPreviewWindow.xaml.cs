using System.Windows;
using System.Windows.Input;
using OutlastTrialsMod.Helpers;

namespace OutlastTrialsMod.Views;

public partial class TextPreviewWindow : Window
{
    public TextPreviewWindow(string title, string content)
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        ContentTextBox.Text = content;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        WindowChromeHelper.OnTitleBarMouseLeftButtonDown(this, e);

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.Minimize(this);

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.ToggleMaximize(this);

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.Close(this);
}
