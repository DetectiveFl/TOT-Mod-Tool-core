using System.IO;
using CUE4Parse.FileProvider.Objects;
using Newtonsoft.Json;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Models;

namespace OutlastTrialsMod.Services;

public sealed class AssetExportService
{
    private static readonly string[] PackageCompanionExtensions = ["uexp", "ubulk", "uptnl"];

    public AssetExportResult SaveRawAsset(string vfsPath, string outputDirectory)
    {
        var provider = Cue4ParseService.Instance.Provider;
        if (provider is null)
            return AssetExportResult.Failed("File provider is not initialized.");

        if (!provider.TryGetGameFile(vfsPath, out var gameFile))
            return AssetExportResult.Failed($"File not found in mounted archives: {vfsPath}");

        try
        {
            Directory.CreateDirectory(outputDirectory);
            var savedPaths = new List<string>();
            WriteGameFile(gameFile, outputDirectory, savedPaths);

            if (VirtualFileTreeBuilder.IsPackageExtension(gameFile.Extension))
            {
                var extensionLength = gameFile.Extension.Length + 1;
                var basePath = vfsPath[..^extensionLength];

                foreach (var companionExtension in PackageCompanionExtensions)
                {
                    var companionPath = $"{basePath}.{companionExtension}";
                    if (provider.TryGetGameFile(companionPath, out var companion))
                        WriteGameFile(companion, outputDirectory, savedPaths);
                }
            }

            if (savedPaths.Count == 0)
                return AssetExportResult.Failed("No files were written.");

            var fileList = string.Join(Environment.NewLine, savedPaths);
            return AssetExportResult.Ok(
                $"Saved {savedPaths.Count} file(s) to:{Environment.NewLine}{fileList}",
                savedPaths.Count);
        }
        catch (Exception ex)
        {
            return AssetExportResult.Failed($"Failed to save raw asset: {ex.Message}");
        }
    }

    public async Task<AssetExportResult> ExportConvertedAsync(
        GameFileItem item,
        string outputDirectory,
        string? exportType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(exportType) ||
            exportType.Equals(item.Extension, StringComparison.OrdinalIgnoreCase))
        {
            exportType = await Cue4ParseService.Instance
                .TryResolveExportTypeAsync(item.Path, cancellationToken)
                .ConfigureAwait(false);
        }

        if (AssetTypeFilter.IsRawTextExtension(item.Extension))
            return await ExportConfigAsJsonAsync(item, outputDirectory, cancellationToken).ConfigureAwait(false);

        if (AssetTypeFilter.IsTextureExport(exportType))
            return await ExportTextureAsPngAsync(item, outputDirectory, cancellationToken).ConfigureAwait(false);

        if (VirtualFileTreeBuilder.IsPackageExtension(item.Extension))
        {
            if (AssetTypeFilter.ShouldHide(exportType))
            {
                return AssetExportResult.Failed(
                    $"Converted export is not supported for asset type: {exportType ?? "Unknown"}");
            }

            return await ExportPackageAsJsonAsync(item, outputDirectory, cancellationToken).ConfigureAwait(false);
        }

        return AssetExportResult.Failed(
            $"Converted export is not supported for: {exportType ?? item.Extension}");
    }

    private static void WriteGameFile(GameFile gameFile, string outputDirectory, ICollection<string> savedPaths)
    {
        var fileName = Path.GetFileName(gameFile.Path);
        var destination = Path.Combine(outputDirectory, fileName);
        File.WriteAllBytes(destination, gameFile.Read());
        savedPaths.Add(destination);
    }

    private static async Task<AssetExportResult> ExportTextureAsPngAsync(
        GameFileItem item,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, $"{item.DisplayName}.png");

            var result = await Cue4ParseService.Instance
                .ExportTextureToPngAsync(item.Path, outputPath, cancellationToken)
                .ConfigureAwait(false);

            if (!result.ExportSucceeded)
                return AssetExportResult.Failed(result.PreviewHint);

            return AssetExportResult.Ok($"Exported PNG to:{Environment.NewLine}{outputPath}");
        }
        catch (Exception ex)
        {
            return AssetExportResult.Failed($"Failed to export texture: {ex.Message}");
        }
    }

    private static async Task<AssetExportResult> ExportPackageAsJsonAsync(
        GameFileItem item,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        try
        {
            var serializeResult = await Cue4ParseService.Instance
                .TrySerializeAssetAsync(item.Path, cancellationToken)
                .ConfigureAwait(false);

            if (!serializeResult.Succeeded)
                return AssetExportResult.Failed(serializeResult.ErrorMessage ?? "Serialization failed.");

            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, $"{item.DisplayName}.json");
            await File.WriteAllTextAsync(outputPath, serializeResult.Text!, cancellationToken).ConfigureAwait(false);

            return AssetExportResult.Ok($"Exported JSON to:{Environment.NewLine}{outputPath}");
        }
        catch (Exception ex)
        {
            return AssetExportResult.Failed($"Failed to export JSON: {ex.Message}");
        }
    }

    private static async Task<AssetExportResult> ExportConfigAsJsonAsync(
        GameFileItem item,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        try
        {
            var textResult = await Cue4ParseService.Instance
                .TryReadRawTextAsync(item.Path, cancellationToken)
                .ConfigureAwait(false);

            if (!textResult.Succeeded)
                return AssetExportResult.Failed(textResult.ErrorMessage ?? "Failed to read config file.");

            var payload = new
            {
                item.Path,
                item.Extension,
                Content = textResult.Text
            };

            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, $"{item.DisplayName}.json");
            var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
            await File.WriteAllTextAsync(outputPath, json, cancellationToken).ConfigureAwait(false);

            return AssetExportResult.Ok($"Exported config JSON to:{Environment.NewLine}{outputPath}");
        }
        catch (Exception ex)
        {
            return AssetExportResult.Failed($"Failed to export config: {ex.Message}");
        }
    }
}
