using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OutlastTrialsMod.Config;

namespace OutlastTrialsMod.Helpers;

public static class IconHelper
{
    private static ImageSource? _folderIcon;
    private static ImageSource? _fileIcon;

    public static ImageSource? FolderIcon => _folderIcon ??= LoadIcon(AppConfig.FolderIconPath);
    public static ImageSource? FileIcon => _fileIcon ??= LoadIcon(AppConfig.FileIconPath);

    private static ImageSource? LoadIcon(string path)
    {
        if (!File.Exists(path)) return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
