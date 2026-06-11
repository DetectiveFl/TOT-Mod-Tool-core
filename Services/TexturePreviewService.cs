using System.IO;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;
using OutlastTrialsMod.Config;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Models;
using SkiaSharp;
using EPixelFormat = CUE4Parse.UE4.Assets.Exports.Texture.EPixelFormat;

namespace OutlastTrialsMod.Services;

public sealed class TexturePreviewService
{
    public string? TryResolveExportType(string vfsPath)
    {
        var provider = Cue4ParseService.Instance.Provider;
        if (provider is null)
            return null;

        if (!provider.TryGetGameFile(vfsPath, out var gameFile))
            return null;

        try
        {
            var package = provider.LoadPackage(gameFile);
            ResolveMainExport(package, gameFile, out var exportType, out _);
            return exportType;
        }
        catch
        {
            return null;
        }
    }

    public TextureDecodeResult TryDecodeTexture(string vfsPath, bool limitPreviewSize, bool retainBitmap = false)
    {
        var provider = Cue4ParseService.Instance.Provider
            ?? throw new InvalidOperationException("File provider is not initialized.");

        if (!vfsPath.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase) &&
            !vfsPath.EndsWith(".umap", StringComparison.OrdinalIgnoreCase))
        {
            return TextureDecodeResult.Failed("Preview is available for .uasset / .umap packages only.");
        }

        if (!provider.TryGetGameFile(vfsPath, out var gameFile))
        {
            return TextureDecodeResult.Failed($"Package not found in mounted archives: {vfsPath}");
        }

        return TryDecodeTexture(gameFile, limitPreviewSize, retainBitmap);
    }

    public TextureDecodeResult TryDecodeTexture(GameFile gameFile, bool limitPreviewSize, bool retainBitmap = false)
    {
        var provider = Cue4ParseService.Instance.Provider
            ?? throw new InvalidOperationException("File provider is not initialized.");

        IPackage package;
        try
        {
            package = provider.LoadPackage(gameFile);
        }
        catch (Exception ex)
        {
            return TextureDecodeResult.Failed($"Failed to load package: {ex.Message}");
        }

        var exported = ResolveMainExport(package, gameFile, out var exportType, out var className);
        if (exported is null)
        {
            return TextureDecodeResult.Failed(
                string.IsNullOrEmpty(exportType)
                    ? $"No export found for '{gameFile.NameWithoutExtension}' in package."
                    : $"Loaded object type: {exportType}",
                exportType,
                className);
        }

        exportType ??= exported.ExportType;
        className ??= exported.GetType().Name;

        if (exported is not UTexture texture)
        {
            return TextureDecodeResult.Failed(
                $"Loaded object type: {exportType}",
                exportType,
                className);
        }

        if (texture.Format == EPixelFormat.PF_BC6H)
        {
            return TextureDecodeResult.Failed(
                $"Loaded object type: {exportType}. BC6H is not supported for preview.",
                exportType,
                className);
        }

        CTexture? decoded;
        try
        {
            decoded = limitPreviewSize
                ? texture.Decode(AppConfig.ThumbnailTextureSize, ETexturePlatform.DesktopMobile)
                : texture.Decode(AppConfig.MaxPreviewTextureSize, ETexturePlatform.DesktopMobile);
        }
        catch (Exception ex)
        {
            return TextureDecodeResult.Failed(
                BuildDecodeFailureMessage(ex, exportType),
                exportType,
                className);
        }

        if (decoded is null)
        {
            return TextureDecodeResult.Failed(
                $"Loaded object type: {exportType}. Decode returned no pixel data (format: {texture.Format}).",
                exportType,
                className);
        }

        SKBitmap skBitmap;
        try
        {
            skBitmap = decoded.ToSkBitmap();
        }
        catch (Exception ex)
        {
            return TextureDecodeResult.Failed(
                $"Loaded object type: {exportType}. {ex.Message}",
                exportType,
                className);
        }

        if (retainBitmap)
        {
            return new TextureDecodeResult
            {
                Bitmap = skBitmap,
                ExportType = exportType,
                ClassName = className
            };
        }

        return TextureDecodeResult.FromBitmap(skBitmap, exportType, className);
    }

    /// <summary>
    /// Decodes on a background thread and converts to WPF <see cref="System.Windows.Media.Imaging.BitmapImage"/>.
    /// </summary>
    public (int Width, int Height)? TryGetTextureDimensions(string vfsPath) =>
        TryGetTextureInjectionMetadata(vfsPath, localUassetPath: null) is { } metadata
            ? (metadata.HeaderWidth, metadata.HeaderHeight)
            : null;

    public TextureInjectionMetadata? TryGetTextureInjectionMetadata(string vfsPath, string? localUassetPath)
    {
        var provider = Cue4ParseService.Instance.Provider;
        if (provider is null)
            return null;

        if (!string.IsNullOrWhiteSpace(localUassetPath) && File.Exists(localUassetPath))
        {
            try
            {
                var localPackage = LoadLocalPackage(localUassetPath, provider);
                var localMetadata = ExtractTextureInjectionMetadata(localPackage);
                if (localMetadata is not null)
                    return localMetadata;
            }
            catch
            {
                // Fall back to mounted archive metadata.
            }
        }

        if (!provider.TryGetGameFile(vfsPath, out var gameFile))
            return null;

        try
        {
            var package = provider.LoadPackage(gameFile);
            return ExtractTextureInjectionMetadata(package);
        }
        catch
        {
            return null;
        }
    }

    private static Package LoadLocalPackage(string uassetPath, IFileProvider provider)
    {
        var basePath = Path.Combine(
            Path.GetDirectoryName(uassetPath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(uassetPath));

        var uassetBytes = File.ReadAllBytes(uassetPath);
        var uexpPath = basePath + ".uexp";
        var ubulkPath = basePath + ".ubulk";
        var uptnlPath = basePath + ".uptnl";

        byte[]? uexpBytes = File.Exists(uexpPath) ? File.ReadAllBytes(uexpPath) : null;
        byte[]? ubulkBytes = File.Exists(ubulkPath) ? File.ReadAllBytes(ubulkPath) : null;
        byte[]? uptnlBytes = File.Exists(uptnlPath) ? File.ReadAllBytes(uptnlPath) : null;

        return new Package(
            Path.GetFileNameWithoutExtension(uassetPath),
            uassetBytes,
            uexpBytes,
            ubulkBytes,
            uptnlBytes,
            provider);
    }

    private static TextureInjectionMetadata? ExtractTextureInjectionMetadata(IPackage package)
    {
        foreach (var export in package.GetExports())
        {
            if (export is not UTexture2D texture2D)
                continue;

            var headerWidth = texture2D.PlatformData.SizeX;
            var headerHeight = texture2D.PlatformData.SizeY;

            if (headerWidth <= 0 || headerHeight <= 0)
            {
                headerWidth = texture2D.ImportedSize.X;
                headerHeight = texture2D.ImportedSize.Y;
            }

            if (headerWidth <= 0 || headerHeight <= 0)
                continue;

            var mips = texture2D.PlatformData.Mips
                .Select(mip => (mip.SizeX, mip.SizeY))
                .Where(mip => mip.SizeX > 0 && mip.SizeY > 0)
                .ToList();

            return new TextureInjectionMetadata(
                headerWidth,
                headerHeight,
                texture2D.Format.ToString(),
                texture2D.PlatformData.Mips.Length,
                texture2D.SRGB,
                mips);
        }

        foreach (var export in package.GetExports())
        {
            if (export is not UTexture texture)
                continue;

            var headerWidth = texture.PlatformData.SizeX;
            var headerHeight = texture.PlatformData.SizeY;

            if (headerWidth <= 0 || headerHeight <= 0)
                continue;

            var mips = texture.PlatformData.Mips
                .Select(mip => (mip.SizeX, mip.SizeY))
                .Where(mip => mip.SizeX > 0 && mip.SizeY > 0)
                .ToList();

            return new TextureInjectionMetadata(
                headerWidth,
                headerHeight,
                texture.Format.ToString(),
                texture.PlatformData.Mips.Length,
                texture.SRGB,
                mips);
        }

        return null;
    }

    public Task<TextureDecodeResult> TryDecodeTextureAsync(
        string vfsPath,
        bool limitPreviewSize,
        CancellationToken cancellationToken = default) =>
        Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return TryDecodeTexture(vfsPath, limitPreviewSize, retainBitmap: false);
            },
            cancellationToken);

    public async Task<TextureDecodeResult> ExportTextureAsync(
        string vfsPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var result = await Task.Run(
            () => TryDecodeTexture(vfsPath, limitPreviewSize: false, retainBitmap: true),
            cancellationToken).ConfigureAwait(false);

        if (!result.HasDecodedBitmap || result.Bitmap is null)
            return result;

        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var image = SKImage.FromBitmap(result.Bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
                data.SaveTo(stream);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.Bitmap.Dispose();
            return TextureDecodeResult.Failed($"Export failed: {ex.Message}", result.ExportType, result.ClassName);
        }

        result.Bitmap.Dispose();
        return new TextureDecodeResult
        {
            ExportType = result.ExportType,
            ClassName = result.ClassName,
            ExportSucceeded = true,
            DiagnosticMessage = $"Exported to {outputPath}"
        };
    }

    private static UObject? ResolveMainExport(
        IPackage package,
        GameFile gameFile,
        out string? exportType,
        out string? className)
    {
        exportType = null;
        className = null;

        var export = AssetTextPreviewService.ResolveMainExport(package, gameFile);
        if (export is null)
            return null;

        exportType = export.ExportType;
        className = export.GetType().Name;
        return export;
    }

    private static string BuildDecodeFailureMessage(Exception ex, string? exportType)
    {
        var typeLabel = string.IsNullOrEmpty(exportType) ? "texture" : exportType;

        if (IsMappingsRelatedFailure(ex))
        {
            // MAPPINGS: Re-enable .usmap in Cue4ParseService.ApplyMappings() when Mappings.usmap is valid.
            return $"Loaded object type: {typeLabel}. Decode failed (mappings not loaded): {ex.Message}";
        }

        return $"Loaded object type: {typeLabel}. Decode failed: {ex.Message}";
    }

    private static bool IsMappingsRelatedFailure(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            var text = current.Message;
            if (text.Contains("mapping", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("usmap", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("MappingsContainer", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("property tag", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("FName", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
