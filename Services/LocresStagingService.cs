using System.IO;
using OutlastTrialsMod.Helpers;

namespace OutlastTrialsMod.Services;

public static class LocresStagingService
{
    public static string? ResolveSourcePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        if (ModStagingPaths.IsWithinModifiedFilesRoot(filePath))
            return Path.GetFullPath(filePath);

        var mirroredPath = ModStagingPaths.GetMirroredLocresPath(filePath);
        if (File.Exists(mirroredPath))
            return mirroredPath;

        var provider = Cue4ParseService.Instance.Provider;
        if (provider is null || !provider.TryGetGameFile(filePath, out var gameFile))
            return null;

        var tempDirectory = Path.Combine(Path.GetTempPath(), "OutlastTrialsMod", "locres");
        Directory.CreateDirectory(tempDirectory);

        var safeName = filePath.Replace('/', '_').Replace('\\', '_');
        var tempPath = Path.Combine(tempDirectory, safeName);
        File.WriteAllBytes(tempPath, gameFile.Read());
        return tempPath;
    }
}
