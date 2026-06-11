namespace OutlastTrialsMod.Models;

public sealed class AssetExportResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public int FilesWritten { get; init; }

    public static AssetExportResult Ok(string message, int filesWritten = 1) =>
        new() { Succeeded = true, Message = message, FilesWritten = filesWritten };

    public static AssetExportResult Failed(string message) =>
        new() { Succeeded = false, Message = message };
}
