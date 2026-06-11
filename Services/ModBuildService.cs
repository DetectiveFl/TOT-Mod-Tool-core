using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Localization;
using OutlastTrialsMod.Models;
using SkiaSharp;

namespace OutlastTrialsMod.Services;

public sealed class ModBuildService
{
    public async Task<ModBuildResult?> BuildModAsync(
        string modName,
        string gameDirectory,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
        var modifiedFilesDir = Path.Combine(toolsDir, "ModifiedFiles");
        var putFilesHereDir = Path.Combine(toolsDir, "put-ur-files-here");
        var outputPakDir = Path.Combine(toolsDir, "output-modifiedpak");
        var pakFileName = $"{modName}_P.pak";

        var loc = LocalizationManager.Instance;

        try
        {
            if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            {
                throw new DirectoryNotFoundException(loc.GameDirectoryMissing);
            }

            var finalModsDir = Path.GetFullPath(gameDirectory);
            var finalPakPath = Path.Combine(finalModsDir, pakFileName);

            progress?.Report(loc.PreparingBuild);

            await ProcessModifiedTexturesAsync(modifiedFilesDir, progress, cancellationToken)
                .ConfigureAwait(false);

            var result = await Task.Run(async () =>
            {
                EnsureEmptyDirectory(outputPakDir);
                Directory.CreateDirectory(modifiedFilesDir);

                progress?.Report(loc.InjectingTextures);
                await InjectPngPayloadIntoUbulkAsync(
                    modifiedFilesDir,
                    toolsDir,
                    progress,
                    cancellationToken).ConfigureAwait(false);

                progress?.Report(loc.CopyingForRepacker);

                if (Directory.Exists(putFilesHereDir))
                    Directory.Delete(putFilesHereDir, recursive: true);

                Directory.CreateDirectory(putFilesHereDir);

                var copiedCount = 0;

                if (Directory.Exists(modifiedFilesDir))
                {
                    foreach (var sourceFile in Directory.GetFiles(modifiedFilesDir, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var extension = Path.GetExtension(sourceFile);
                            if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                                extension.Equals(".dds", StringComparison.OrdinalIgnoreCase) ||
                                extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            var relativePath = Path.GetRelativePath(modifiedFilesDir, sourceFile);
                            var destFile = Path.Combine(putFilesHereDir, relativePath);
                            var destDirectory = Path.GetDirectoryName(destFile);

                            if (!string.IsNullOrEmpty(destDirectory))
                                Directory.CreateDirectory(destDirectory);

                            File.Copy(sourceFile, destFile, overwrite: true);
                            copiedCount++;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(loc.Format(nameof(LocalizationManager.FileError), sourceFile, ex.Message), ex);
                        }
                    }
                }

                if (copiedCount == 0)
                {
                    throw new Exception(loc.Format(nameof(LocalizationManager.ZeroFilesCopied), modifiedFilesDir));
                }

                if (!TryCreateRepackerStartInfo(toolsDir, out var repackerStartInfo))
                {
                    throw new FileNotFoundException(loc.Format(nameof(LocalizationManager.RepackerNotFound), toolsDir));
                }

                progress?.Report(loc.PackingMod);
                await RunRepackerAsync(repackerStartInfo, loc, cancellationToken).ConfigureAwait(false);

                progress?.Report(loc.SavingMod);

                var generatedPaks = Directory.GetFiles(outputPakDir, "*.pak");
                if (generatedPaks.Length == 0)
                    throw new FileNotFoundException(loc.RepackerNoPak);

                var sourcePakPath = generatedPaks[0];
                finalPakPath = Path.Combine(finalModsDir, modName + "_P.pak");

                if (File.Exists(finalPakPath))
                    File.Delete(finalPakPath);

                File.Move(sourcePakPath, finalPakPath);

                ClearDirectoryContents(putFilesHereDir);
                ClearDirectoryContents(outputPakDir);

                return new ModBuildResult(true, finalPakPath, pakFileName);
            }, cancellationToken).ConfigureAwait(false);

            ShowMessageOnUiThread(
                loc.Format(nameof(LocalizationManager.ModBuildSuccess), pakFileName),
                loc.Success,
                MessageBoxImage.Information);

            return result;
        }
        catch (Exception ex)
        {
            ShowMessageOnUiThread(ex.Message, loc.CreateMod, MessageBoxImage.Error);
            return null;
        }
        finally
        {
            TryCleanupWorkspace(putFilesHereDir, outputPakDir);
        }
    }

    private static void TryCleanupWorkspace(string putFilesHereDir, string outputPakDir)
    {
        try
        {
            ClearDirectoryContents(putFilesHereDir);
            ClearDirectoryContents(outputPakDir);
        }
        catch
        {
            // Best-effort cleanup; never block UI recovery.
        }
    }

    private static void ShowMessageOnUiThread(string message, string title, MessageBoxImage icon)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            return;
        }

        dispatcher.Invoke(
            () => MessageBox.Show(message, title, MessageBoxButton.OK, icon),
            DispatcherPriority.Normal);
    }

    private static void EnsureEmptyDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
            Directory.Delete(directoryPath, recursive: true);

        Directory.CreateDirectory(directoryPath);
    }

    private static void ClearDirectoryContents(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            return;
        }

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            File.Delete(filePath);

        foreach (var subDirectory in Directory
                     .EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            Directory.Delete(subDirectory);
        }
    }

    private static bool TryCreateRepackerStartInfo(string toolsDir, out ProcessStartInfo startInfo)
    {
        var pakBat = Path.Combine(toolsDir, "Pak.bat");
        if (File.Exists(pakBat))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = pakBat,
                WorkingDirectory = toolsDir,
                CreateNoWindow = false,
                UseShellExecute = true
            };
            return true;
        }

        var repackerExe = Path.Combine(toolsDir, "repacker.exe");
        if (File.Exists(repackerExe))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = repackerExe,
                WorkingDirectory = toolsDir,
                CreateNoWindow = false,
                UseShellExecute = true
            };
            return true;
        }

        startInfo = null!;
        return false;
    }

    private static async Task RunRepackerAsync(
        ProcessStartInfo startInfo,
        LocalizationManager loc,
        CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
            throw new InvalidOperationException(loc.RepackerStartFailed);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                loc.Format(nameof(LocalizationManager.RepackerExitCode), process.ExitCode));
        }
    }

    private static async Task ProcessModifiedTexturesAsync(
        string modifiedFilesRoot,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(modifiedFilesRoot))
            return;

        var pngFiles = Directory
            .EnumerateFiles(modifiedFilesRoot, "*.png", SearchOption.AllDirectories)
            .ToList();

        if (pngFiles.Count == 0)
            return;

        if (Cue4ParseService.Instance.Provider is null)
            throw new InvalidOperationException("File provider is not initialized. Open the game directory first.");

        foreach (var pngPath in pngFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var vfsPath = ResolveVfsPathFromModifiedPng(pngPath, modifiedFilesRoot);
            var outputUassetPath = GetMirroredAssetPath(modifiedFilesRoot, vfsPath);
            var outputDirectory = Path.GetDirectoryName(outputUassetPath)!;
            Directory.CreateDirectory(outputDirectory);

            progress?.Report(LocalizationManager.Instance.Format(
                nameof(LocalizationManager.PreparingFile), Path.GetFileName(pngPath)));

            await EnsureOriginalUassetExtractedAsync(vfsPath, outputDirectory, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task InjectPngPayloadIntoUbulkAsync(
        string modifiedFilesDir,
        string toolsDir,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var texconvPath = Path.Combine(toolsDir, "texconv.exe");

        if (!File.Exists(texconvPath))
            throw new FileNotFoundException($"texconv.exe not found: {texconvPath}");

        if (!Directory.Exists(modifiedFilesDir))
            return;

        if (Cue4ParseService.Instance.Provider is null)
            throw new InvalidOperationException("File provider is not initialized. Open the game directory first.");

        foreach (var pngFile in Directory.GetFiles(modifiedFilesDir, "*.png", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullPngPath = Path.GetFullPath(pngFile);
            var uassetFile = Path.ChangeExtension(fullPngPath, ".uasset");
            var uexpFile = Path.ChangeExtension(fullPngPath, ".uexp");
            var ubulkFile = Path.ChangeExtension(fullPngPath, ".ubulk");

            if (!File.Exists(uassetFile) || (!File.Exists(uexpFile) && !File.Exists(ubulkFile)))
            {
                Debug.WriteLine(
                    $"[Inject] Skipping {fullPngPath}: missing companion .uasset/.uexp/.ubulk files.");
                continue;
            }

            progress?.Report(LocalizationManager.Instance.Format(
                nameof(LocalizationManager.InjectingFile), Path.GetFileName(pngFile)));

            var vfsPath = ResolveVfsPathFromModifiedPng(fullPngPath, modifiedFilesDir);
            var localUassetPath = uassetFile;
            var metadata = await Cue4ParseService.Instance
                .TryGetTextureInjectionMetadataAsync(vfsPath, localUassetPath, cancellationToken)
                .ConfigureAwait(false);

            if (metadata is null)
            {
                throw new InvalidOperationException(
                    LocalizationManager.Instance.Format(nameof(LocalizationManager.TextureMetadataFailed), vfsPath));
            }

            var originalPixelFormat = metadata.PixelFormat;
            var originalMipCount = metadata.MipCount;
            var isSrgb = metadata.IsSrgb;
            Debug.WriteLine(
                $"[TextureMetadata] {vfsPath}: PixelFormat={originalPixelFormat}, " +
                $"MipCount={originalMipCount}, SRGB={isSrgb}");

            ResizePngToExactDimensions(fullPngPath, metadata.HeaderWidth, metadata.HeaderHeight);

            var pngDirectory = Path.GetDirectoryName(fullPngPath)
                ?? throw new InvalidOperationException($"PNG path has no directory: {fullPngPath}");

            var ddsPath = Path.ChangeExtension(fullPngPath, ".dds");
            var texconvFormat = DdsPayloadReader.MapPixelFormatToTexconvArgument(
                originalPixelFormat,
                isSrgb);
            Debug.WriteLine(
                $"[Texconv] Source PNG (user modified): {fullPngPath}");
            Debug.WriteLine(
                $"[Texconv] PNG last modified (UTC): {File.GetLastWriteTimeUtc(fullPngPath):O}");
            Debug.WriteLine(
                $"[Texconv] {vfsPath}: -f {texconvFormat} -m {originalMipCount}");

            await RunTexconvAsync(
                texconvPath,
                fullPngPath,
                pngDirectory,
                texconvFormat,
                originalMipCount,
                cancellationToken).ConfigureAwait(false);

            if (!File.Exists(ddsPath))
                throw new FileNotFoundException($"texconv did not create DDS file: {ddsPath}", ddsPath);

            UAssetTextureInjector.InjectDdsIntoTextureAsset(localUassetPath, ddsPath, metadata);

            File.Delete(ddsPath);
        }
    }

    private static async Task RunTexconvAsync(
        string texconvPath,
        string pngFile,
        string outputDirectory,
        string texconvFormat,
        int mipCount,
        CancellationToken cancellationToken)
    {
        var sourcePngPath = Path.GetFullPath(pngFile);
        var mipArgument = Math.Max(1, mipCount);
        var baseArguments = $"-y -m {mipArgument} -f {texconvFormat} -o \"{outputDirectory}\" \"{sourcePngPath}\"";
        var pxArguments = $"{baseArguments} -px";

        Debug.WriteLine($"[Texconv] Executing: {texconvPath} {pxArguments}");

        if (await TryRunTexconvAsync(texconvPath, pxArguments, sourcePngPath, cancellationToken).ConfigureAwait(false))
            return;

        Debug.WriteLine($"[Texconv] Retrying without -px: {texconvPath} {baseArguments}");

        if (!await TryRunTexconvAsync(texconvPath, baseArguments, sourcePngPath, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"texconv failed for {sourcePngPath} with and without -px.");
        }
    }

    private static async Task<bool> TryRunTexconvAsync(
        string texconvPath,
        string arguments,
        string pngFile,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = texconvPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false
            }
        };

        if (!process.Start())
            throw new InvalidOperationException($"Failed to start texconv for: {Path.GetFullPath(pngFile)}");

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode == 0;
    }

    private static void ResizePngToExactDimensions(string pngFile, int targetWidth, int targetHeight)
    {
        using var sourceBitmap = SKBitmap.Decode(pngFile);
        if (sourceBitmap is null)
            throw new InvalidOperationException($"Failed to decode PNG: {pngFile}");

        if (sourceBitmap.Width == targetWidth && sourceBitmap.Height == targetHeight)
            return;

        using var resizedBitmap = sourceBitmap.Resize(
            new SKImageInfo(targetWidth, targetHeight, sourceBitmap.ColorType, sourceBitmap.AlphaType),
            SKFilterQuality.High);

        if (resizedBitmap is null)
            throw new InvalidOperationException($"Failed to resize PNG: {pngFile}");

        using var image = SKImage.FromBitmap(resizedBitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        if (encoded is null)
            throw new InvalidOperationException($"Failed to encode resized PNG: {pngFile}");

        using var stream = File.Open(pngFile, FileMode.Create, FileAccess.Write, FileShare.Read);
        encoded.SaveTo(stream);
    }

    private static string ResolveVfsPathFromModifiedPng(string pngPath, string modifiedFilesRoot)
    {
        var fullPngPath = Path.GetFullPath(pngPath);
        var fullRoot = Path.GetFullPath(modifiedFilesRoot);

        if (!fullPngPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !fullPngPath.Equals(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"PNG is outside ModifiedFiles: {pngPath}");
        }

        var relativePath = Path.GetRelativePath(fullRoot, fullPngPath);
        var vfsPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
        return Path.ChangeExtension(vfsPath, ".uasset");
    }

    private static string GetMirroredAssetPath(string modifiedFilesRoot, string vfsPath)
    {
        var normalized = vfsPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(modifiedFilesRoot, normalized);
    }

    private static async Task EnsureOriginalUassetExtractedAsync(
        string vfsPath,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        var uassetFileName = Path.GetFileName(vfsPath);
        var cachedUassetPath = Path.Combine(outputDirectory, uassetFileName);

        if (File.Exists(cachedUassetPath))
            return;

        var result = await Cue4ParseService.Instance
            .SaveRawAssetAsync(vfsPath, outputDirectory, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
            throw new InvalidOperationException(result.Message);
    }
}
