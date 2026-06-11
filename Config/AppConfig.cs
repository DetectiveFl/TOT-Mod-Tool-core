using System.IO;

namespace OutlastTrialsMod.Config;

public static class AppConfig
{
    public const string AesKey =
        "0x613E92E0F3CE880FC652EC86254E2581126AE86D63BA46550FB2CE0EC2EDA439";

    /// <summary>Max edge length for grid thumbnails.</summary>
    public const int ThumbnailTextureSize = 256;

    /// <summary>Max edge length for pop-out preview window.</summary>
    public const int MaxPreviewTextureSize = 2048;

    public static string MappingsPath =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Mappings.usmap");

    public static string IconsDirectory =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Icons");

    public static string FolderIconPath => Path.Combine(IconsDirectory, "folder.png");
    public static string FileIconPath => Path.Combine(IconsDirectory, "file.png");
}
