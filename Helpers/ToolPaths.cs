using System.IO;
using System.Reflection;

namespace OutlastTrialsMod.Helpers;

public static class ToolPaths
{
    public static string GetExecutableBaseDirectory()
    {
        var exeLocation = Assembly.GetExecutingAssembly().Location;
        var baseDir = Path.GetDirectoryName(exeLocation);

        if (!string.IsNullOrEmpty(baseDir))
            return Path.GetFullPath(baseDir);

        return Path.GetFullPath(AppContext.BaseDirectory);
    }

    public static string ResolveToolPath(string toolFileName)
    {
        var baseDir = GetExecutableBaseDirectory();
        return Path.GetFullPath(Path.Combine(baseDir, "Tools", toolFileName));
    }

    public static string GetRepackerPath() => ResolveToolPath("repacker.exe");

    public static string GetTexconvPath() => ResolveToolPath("texconv.exe");
}
