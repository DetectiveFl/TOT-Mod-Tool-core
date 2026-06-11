using System.Windows;
using System.Windows.Media;

namespace OutlastTrialsMod.Helpers;

public static class AssetTypeStyle
{
    public const string DefaultColor = "#7f8c8d";
    public const string TextureColor = "#9b59b6";
    public const string MaterialColor = "#e67e22";
    public const string BlueprintColor = "#2980b9";
    public const string MediaColor = "#e74c3c";

    public static SolidColorBrush GetAccentBrush(string? exportType)
    {
        var color = (Color)ColorConverter.ConvertFromString(GetAccentHex(exportType))!;
        var brush = new SolidColorBrush(color);
        if (brush.CanFreeze) brush.Freeze();
        return brush;
    }

    public static string GetAccentHex(string? exportType)
    {
        if (string.IsNullOrWhiteSpace(exportType))
            return DefaultColor;

        if (IsTextureType(exportType))
            return TextureColor;

        if (IsMaterialType(exportType))
            return MaterialColor;

        if (IsBlueprintType(exportType))
            return BlueprintColor;

        if (IsMediaType(exportType))
            return MediaColor;

        return DefaultColor;
    }

    public static Geometry GetIconGeometry(string? exportType)
    {
        if (string.IsNullOrWhiteSpace(exportType))
            return FileIcon;

        if (IsTextureType(exportType))
            return ImageIcon;

        if (IsMaterialType(exportType))
            return SphereIcon;

        if (IsBlueprintType(exportType))
            return GearIcon;

        if (IsMediaType(exportType))
            return FilmIcon;

        return FileIcon;
    }

    private static bool IsTextureType(string exportType) =>
        exportType.Contains("Texture2D", StringComparison.OrdinalIgnoreCase) ||
        exportType.Equals("Texture", StringComparison.OrdinalIgnoreCase);

    private static bool IsMaterialType(string exportType) =>
        exportType.Contains("MaterialInstance", StringComparison.OrdinalIgnoreCase) ||
        exportType.Equals("Material", StringComparison.OrdinalIgnoreCase) ||
        exportType.Contains("MaterialInstanceConstant", StringComparison.OrdinalIgnoreCase);

    private static bool IsBlueprintType(string exportType) =>
        exportType.Contains("Blueprint", StringComparison.OrdinalIgnoreCase);

    private static bool IsMediaType(string exportType) =>
        exportType.Contains("FileMediaSource", StringComparison.OrdinalIgnoreCase) ||
        exportType.Contains("MediaPlayer", StringComparison.OrdinalIgnoreCase);

    private static Geometry Parse(string pathData)
    {
        var geometry = Geometry.Parse(pathData);
        geometry.Freeze();
        return geometry;
    }

    private static readonly Geometry FileIcon = Parse(
        "M5,2 L15,2 L19,6 L19,20 L5,20 Z M15,2 L15,6 L19,6");

    private static readonly Geometry ImageIcon = Parse(
        "M4,4 L20,4 L20,20 L4,20 Z M7,15 L11,11 L14,14 L17,9 L17,17 L7,17 Z M8,8 A1.5,1.5 0 1 0 8,11 A1.5,1.5 0 1 0 8,8");

    private static readonly Geometry SphereIcon = Parse(
        "M12,3 A9,9 0 1 0 12,21 A9,9 0 1 0 12,3 M3,12 A9,4 0 0 0 21,12 A9,4 0 0 0 3,12");

    private static readonly Geometry GearIcon = Parse(
        "M12,8 A4,4 0 1 0 12,16 A4,4 0 1 0 12,8 M12,2 L13,5 L16,4 L17,7 L20,8 L19,11 L21,13 L18,14 L17,17 L14,16 L13,19 L11,19 L10,16 L7,17 L6,14 L3,13 L5,11 L4,8 L7,7 L8,4 L11,5 Z");

    private static readonly Geometry FilmIcon = Parse(
        "M4,5 L20,5 L20,19 L4,19 Z M7,5 L7,19 M17,5 L17,19 M4,9 L20,9 M4,15 L20,15");
}
