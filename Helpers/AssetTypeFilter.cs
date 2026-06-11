namespace OutlastTrialsMod.Helpers;

public static class AssetTypeFilter
{
    private static readonly HashSet<string> RawTextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ini",
        "txt"
    };

    private static readonly string[] HiddenMeshTokens =
    [
        "StaticMesh",
        "SkeletalMesh",
        "Skeleton",
        "PhysicsAsset",
        "AnimSequence",
        "AnimMontage",
        "PoseAsset",
        "BlendSpace",
        "NiagaraSystem",
        "NiagaraEmitter",
        "ParticleSystem",
        "LevelSequence",
        "World",
        "Level",
        "Landscape",
        "SplineMesh",
        "GeometryCollection"
    ];

    public static bool IsLocresExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        return extension.TrimStart('.').Equals("locres", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRawTextExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        var normalized = extension.TrimStart('.');
        return RawTextExtensions.Contains(normalized);
    }

    public static bool ShouldHide(string? exportType, string? className = null)
    {
        if (!string.IsNullOrWhiteSpace(exportType))
        {
            foreach (var token in HiddenMeshTokens)
            {
                if (exportType.Contains(token, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(className))
        {
            foreach (var token in HiddenMeshTokens)
            {
                if (className.Contains(token, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    public static bool IsTextDataExport(string? exportType)
    {
        if (string.IsNullOrWhiteSpace(exportType))
            return false;

        return exportType.Contains("Blueprint", StringComparison.OrdinalIgnoreCase) ||
               exportType.Contains("DataTable", StringComparison.OrdinalIgnoreCase) ||
               exportType.Contains("StringTable", StringComparison.OrdinalIgnoreCase) ||
               exportType.Contains("CurveTable", StringComparison.OrdinalIgnoreCase) ||
               exportType.Contains("Material", StringComparison.OrdinalIgnoreCase) ||
               exportType.Contains("Media", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTextureExport(string? exportType) =>
        !string.IsNullOrWhiteSpace(exportType) &&
        (exportType.Contains("Texture2D", StringComparison.OrdinalIgnoreCase) ||
         exportType.Equals("Texture", StringComparison.OrdinalIgnoreCase));
}
