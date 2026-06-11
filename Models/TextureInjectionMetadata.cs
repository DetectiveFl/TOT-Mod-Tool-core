namespace OutlastTrialsMod.Models;

public sealed class TextureInjectionMetadata
{
    public TextureInjectionMetadata(
        int headerWidth,
        int headerHeight,
        string pixelFormat,
        int mipCount,
        bool isSrgb,
        IReadOnlyList<(int SizeX, int SizeY)> mips)
    {
        HeaderWidth = headerWidth;
        HeaderHeight = headerHeight;
        PixelFormat = pixelFormat;
        MipCount = mipCount;
        IsSrgb = isSrgb;
        Mips = mips;
    }

    public int HeaderWidth { get; }
    public int HeaderHeight { get; }
    public string PixelFormat { get; }
    public int MipCount { get; }
    public bool IsSrgb { get; }
    public IReadOnlyList<(int SizeX, int SizeY)> Mips { get; }

    public (int Width, int Height) ResolveInjectionDimensions(long ubulkLength)
    {
        var bpp = GetBytesPerPixel(PixelFormat);

        foreach (var (sizeX, sizeY) in Mips)
        {
            if (sizeX <= 0 || sizeY <= 0)
                continue;

            var expectedBytes = sizeX * sizeY * bpp;
            if (expectedBytes <= ubulkLength)
                return (sizeX, sizeY);
        }

        return (HeaderWidth, HeaderHeight);
    }

    public static double GetBytesPerPixel(string pixelFormat)
    {
        if (pixelFormat.Contains("DXT1", StringComparison.OrdinalIgnoreCase) ||
            pixelFormat.Contains("BC1", StringComparison.OrdinalIgnoreCase) ||
            pixelFormat.Contains("BC4", StringComparison.OrdinalIgnoreCase))
        {
            return 0.5;
        }

        if (pixelFormat.Contains("DXT5", StringComparison.OrdinalIgnoreCase) ||
            pixelFormat.Contains("BC3", StringComparison.OrdinalIgnoreCase) ||
            pixelFormat.Contains("BC5", StringComparison.OrdinalIgnoreCase) ||
            pixelFormat.Contains("BC7", StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        if (pixelFormat.Contains("B8G8R8A8", StringComparison.OrdinalIgnoreCase))
            return 4.0;

        return 1.0;
    }
}
