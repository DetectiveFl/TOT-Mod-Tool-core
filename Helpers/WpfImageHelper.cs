using System.IO;
using System.Windows.Media.Imaging;
using SkiaSharp;

namespace OutlastTrialsMod.Helpers;

public static class WpfImageHelper
{
    public static BitmapImage? FromSkBitmap(SKBitmap? bitmap)
    {
        if (bitmap is null) return null;

        using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return FromPngBytes(encoded.ToArray());
    }

    public static BitmapImage? FromPngBytes(byte[]? pngBytes)
    {
        if (pngBytes is null || pngBytes.Length == 0)
            return null;

        using var stream = new MemoryStream(pngBytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public static BitmapImage? FromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
