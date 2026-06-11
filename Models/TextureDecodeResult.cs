using System.Windows.Media.Imaging;
using SkiaSharp;

namespace OutlastTrialsMod.Models;

public sealed class TextureDecodeResult
{
    public BitmapImage? Preview { get; init; }
    public SKBitmap? Bitmap { get; init; }
    public string? ExportType { get; init; }
    public string? ClassName { get; init; }
    public string? DiagnosticMessage { get; init; }
    public bool Success => Preview is not null;
    public bool HasDecodedBitmap => Bitmap is not null;
    public bool ExportSucceeded { get; init; }

    /// <summary>Short label for grid cards (type or error).</summary>
    public string CardStatus
    {
        get
        {
            if (Success)
                return ExportType ?? "Texture";

            if (!string.IsNullOrWhiteSpace(ExportType))
                return $"Type: {ExportType}";

            return PreviewHint;
        }
    }

    public string PreviewHint
    {
        get
        {
            if (Success) return ExportType ?? "Texture";
            if (!string.IsNullOrWhiteSpace(DiagnosticMessage)) return DiagnosticMessage;
            return "Could not decode texture.";
        }
    }

    public static TextureDecodeResult Failed(string message, string? exportType = null, string? className = null) =>
        new()
        {
            ExportType = exportType,
            ClassName = className,
            DiagnosticMessage = message
        };

    public static TextureDecodeResult FromBitmap(
        SKBitmap bitmap,
        string? exportType,
        string? className,
        bool disposeBitmap = true)
    {
        var preview = Helpers.WpfImageHelper.FromSkBitmap(bitmap);
        if (disposeBitmap)
            bitmap.Dispose();

        if (preview is null)
        {
            return Failed(
                "Failed to create WPF image from decoded texture.",
                exportType,
                className);
        }

        return new TextureDecodeResult
        {
            Preview = preview,
            ExportType = exportType,
            ClassName = className
        };
    }
}
