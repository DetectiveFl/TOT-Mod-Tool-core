using System.Windows;
using System.Windows.Media;

namespace OutlastTrialsMod.Views;

public partial class ImagePreviewWindow : Window
{
    public ImagePreviewWindow(string title, ImageSource imageSource)
    {
        Title = title;
        InitializeComponent();
        PreviewImage.Source = imageSource;
    }
}
