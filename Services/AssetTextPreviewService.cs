using System.Text;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using Newtonsoft.Json;
using OutlastTrialsMod.Models;

namespace OutlastTrialsMod.Services;

public sealed class AssetTextPreviewService
{
    public TextPreviewResult TryReadRawText(string vfsPath)
    {
        var provider = Cue4ParseService.Instance.Provider;
        if (provider is null)
            return TextPreviewResult.Failed("File provider is not initialized.");

        if (!provider.TryGetGameFile(vfsPath, out var gameFile))
            return TextPreviewResult.Failed($"File not found in mounted archives: {vfsPath}");

        byte[] bytes;
        try
        {
            bytes = gameFile.Read();
        }
        catch (Exception ex)
        {
            return TextPreviewResult.Failed($"Failed to read file: {ex.Message}");
        }

        if (!TryDecodeText(bytes, out var text))
            return TextPreviewResult.Failed("File does not appear to be readable text (UTF-8).");

        return TextPreviewResult.Ok(text);
    }

    public TextPreviewResult TrySerializeAsset(string vfsPath)
    {
        var provider = Cue4ParseService.Instance.Provider;
        if (provider is null)
            return TextPreviewResult.Failed("File provider is not initialized.");

        if (!provider.TryGetGameFile(vfsPath, out var gameFile))
            return TextPreviewResult.Failed($"Package not found in mounted archives: {vfsPath}");

        UObject? exported;
        try
        {
            exported = LoadMainExport(provider, gameFile);
        }
        catch (Exception ex)
        {
            return TextPreviewResult.Failed($"Failed to load asset: {ex.Message}");
        }

        if (exported is null)
            return TextPreviewResult.Failed($"No export found for '{gameFile.NameWithoutExtension}' in package.");

        try
        {
            var json = JsonConvert.SerializeObject(exported, Formatting.Indented);
            return TextPreviewResult.Ok(json);
        }
        catch (Exception ex)
        {
            return TextPreviewResult.Failed($"Failed to serialize asset to JSON: {ex.Message}");
        }
    }

    public Task<TextPreviewResult> TryReadRawTextAsync(string vfsPath, CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TryReadRawText(vfsPath);
        }, cancellationToken);

    public Task<TextPreviewResult> TrySerializeAssetAsync(string vfsPath, CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TrySerializeAsset(vfsPath);
        }, cancellationToken);

    private static UObject? LoadMainExport(AbstractFileProvider provider, GameFile gameFile)
    {
        var package = provider.LoadPackage(gameFile);
        return ResolveMainExport(package, gameFile);
    }

    internal static UObject? ResolveMainExport(IPackage package, GameFile gameFile)
    {
        var candidateNames = new[]
        {
            gameFile.NameWithoutExtension,
            $"{gameFile.NameWithoutExtension}_C"
        };

        foreach (var name in candidateNames)
        {
            var export = package.GetExportOrNull(name, StringComparison.OrdinalIgnoreCase);
            if (export is not null)
                return export;
        }

        if (package.ExportMapLength > 0)
            return package.GetExport(0);

        return null;
    }

    private static bool TryDecodeText(byte[] bytes, out string text)
    {
        text = string.Empty;
        if (bytes.Length == 0)
        {
            text = string.Empty;
            return true;
        }

        if (bytes.Length > 8 * 1024 * 1024)
            return false;

        if (bytes.AsSpan().IndexOf((byte)0) >= 0)
            return false;

        try
        {
            text = Encoding.UTF8.GetString(bytes);
            if (text.Contains('\uFFFD'))
                return false;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
