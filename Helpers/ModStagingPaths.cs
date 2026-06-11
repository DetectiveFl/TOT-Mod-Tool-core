using System.IO;

namespace OutlastTrialsMod.Helpers;

public static class ModStagingPaths
{
    public const string ToolsFolderName = "Tools";
    public const string ModifiedFilesFolderName = "ModifiedFiles";
    public const string PutFilesHereFolderName = "put-ur-files-here";
    public const string RepackerOutputFolderName = "output-modifiedpak";

    public static string GetAppBaseDirectory() =>
        ToolPaths.GetExecutableBaseDirectory();

    public static string GetToolsDirectory() =>
        Path.Combine(GetAppBaseDirectory(), ToolsFolderName);

    public static string GetModifiedFilesRoot() =>
        Path.Combine(GetToolsDirectory(), ModifiedFilesFolderName);

    public static void EnsureModifiedFilesRootExists() =>
        Directory.CreateDirectory(GetModifiedFilesRoot());

    public static string GetPutFilesHereDirectory() =>
        Path.Combine(GetToolsDirectory(), PutFilesHereFolderName);

    public static string GetRepackerOutputDirectory() =>
        Path.Combine(GetToolsDirectory(), RepackerOutputFolderName);

    public static bool IsWithinModifiedFilesRoot(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetFullPath(GetModifiedFilesRoot());
        return fullPath.Equals(root, StringComparison.OrdinalIgnoreCase) ||
               fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryGetRelativeVirtualPath(string localPath, out string virtualPath)
    {
        virtualPath = string.Empty;
        if (string.IsNullOrWhiteSpace(localPath) || !IsWithinModifiedFilesRoot(localPath))
            return false;

        var root = Path.GetFullPath(GetModifiedFilesRoot());
        virtualPath = Path.GetRelativePath(root, Path.GetFullPath(localPath))
            .Replace(Path.DirectorySeparatorChar, '/');
        return true;
    }

    public static string GetMirroredLocresPath(string assetVirtualPath)
    {
        var normalized = assetVirtualPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(GetModifiedFilesRoot(), normalized);
    }

    public static string GetMirroredPngPath(string assetVirtualPath)
    {
        var normalized = assetVirtualPath.Replace('/', Path.DirectorySeparatorChar);
        var assetDirectory = Path.GetDirectoryName(normalized) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(normalized) + ".png";
        return Path.Combine(GetModifiedFilesRoot(), assetDirectory, fileName);
    }

    public static bool HasModifiedFiles()
    {
        var root = GetModifiedFilesRoot();
        return Directory.Exists(root) &&
               Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Any();
    }

    public static string GetRepackerPath() => ToolPaths.GetRepackerPath();

    public static string GetTexconvPath() => ToolPaths.GetTexconvPath();
}
