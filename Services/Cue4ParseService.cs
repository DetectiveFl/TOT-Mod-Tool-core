using System.IO;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Textures;
using OutlastTrialsMod.Config;
using OutlastTrialsMod.Models;

namespace OutlastTrialsMod.Services;

public sealed class Cue4ParseService
{
    public static Cue4ParseService Instance { get; } = new();

    private readonly TexturePreviewService _textures = new();
    private readonly AssetTextPreviewService _textPreview = new();
    private readonly AssetExportService _export = new();

    public AbstractVfsFileProvider? Provider { get; private set; }

    public async Task InitializeAsync(string gameDirectory, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            throw new DirectoryNotFoundException($"Game directory not found: {gameDirectory}");

        try
        {
            progress?.Report("Initializing Oodle...");
            await OodleHelper.InitializeAsync().ConfigureAwait(false);

            progress?.Report("Creating file provider...");
            var versionContainer = new VersionContainer(EGame.GAME_OutlastTrials);
            Provider = new DefaultFileProvider(
                gameDirectory,
                SearchOption.AllDirectories,
                versionContainer,
                StringComparer.OrdinalIgnoreCase);

            // MAPPINGS: .usmap was disabled due to "invalid magic" with the bundled file.
            // When you have a compatible Mappings.usmap for Outlast Trials, call ApplyMappings() here.
            ApplyMappings();

            progress?.Report("Scanning archives...");
            await Task.Run(() => Provider.Initialize()).ConfigureAwait(false);

            progress?.Report("Submitting AES key...");
            var aesKey = new FAesKey(AppConfig.AesKey);
            await Task.Run(() =>
            {
                Provider.SubmitKey(new FGuid(), aesKey);
                Provider.PostMount();
            }).ConfigureAwait(false);

            if (Provider.MountedVfs.Count == 0 && Provider.Files.Count == 0)
                throw new InvalidOperationException(
                    "No archives were mounted. Check the game directory path and AES key.");

            progress?.Report($"Mounted {Provider.MountedVfs.Count} archives, {Provider.Files.Count} files.");
        }
        catch (Exception ex)
        {
            DisposeProvider();
            throw new InvalidOperationException($"CUE4Parse initialization failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// MAPPINGS: Re-implement when a valid Outlast Trials <c>Mappings.usmap</c> is available.
    /// Assign <see cref="AbstractFileProvider.MappingsContainer"/> before mounting archives.
    /// </summary>
    private void ApplyMappings()
    {
        // Uncomment when AppConfig.MappingsPath points to a compatible .usmap:
        //
        // if (Provider is null || !File.Exists(AppConfig.MappingsPath))
        //     return;
        //
        // Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(AppConfig.MappingsPath);
    }

    public Task<string?> TryResolveExportTypeAsync(string vfsPath, CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _textures.TryResolveExportType(vfsPath);
        }, cancellationToken);

    public Task<(int Width, int Height)?> TryGetTextureDimensionsAsync(
        string vfsPath,
        CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _textures.TryGetTextureDimensions(vfsPath);
        }, cancellationToken);

    public Task<TextureInjectionMetadata?> TryGetTextureInjectionMetadataAsync(
        string vfsPath,
        string? localUassetPath,
        CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _textures.TryGetTextureInjectionMetadata(vfsPath, localUassetPath);
        }, cancellationToken);

    public Task<TextureDecodeResult> TryDecodeTextureThumbnailAsync(
        string vfsPath,
        CancellationToken cancellationToken = default) =>
        _textures.TryDecodeTextureAsync(vfsPath, limitPreviewSize: true, cancellationToken);

    public Task<TextureDecodeResult> TryDecodeTextureFullAsync(
        string vfsPath,
        CancellationToken cancellationToken = default) =>
        _textures.TryDecodeTextureAsync(vfsPath, limitPreviewSize: false, cancellationToken);

    public Task<TextPreviewResult> TryReadRawTextAsync(string vfsPath, CancellationToken cancellationToken = default) =>
        _textPreview.TryReadRawTextAsync(vfsPath, cancellationToken);

    public Task<TextPreviewResult> TrySerializeAssetAsync(string vfsPath, CancellationToken cancellationToken = default) =>
        _textPreview.TrySerializeAssetAsync(vfsPath, cancellationToken);

    public Task<AssetExportResult> SaveRawAssetAsync(
        string vfsPath,
        string outputDirectory,
        CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _export.SaveRawAsset(vfsPath, outputDirectory);
        }, cancellationToken);

    public Task<AssetExportResult> ExportConvertedAsync(
        GameFileItem item,
        string outputDirectory,
        string? exportType,
        CancellationToken cancellationToken = default) =>
        _export.ExportConvertedAsync(item, outputDirectory, exportType, cancellationToken);

    public Task<TextureDecodeResult> ExportTextureToPngAsync(
        string vfsPath,
        string outputPath,
        CancellationToken cancellationToken = default) =>
        _textures.ExportTextureAsync(vfsPath, outputPath, cancellationToken);

    public void DisposeProvider()
    {
        if (Provider is null) return;

        try
        {
            Provider.UnloadNonStreamedVfs();
        }
        catch
        {
            // Best-effort cleanup
        }

        Provider = null;
        GC.Collect();
    }
}
