namespace OutlastTrialsMod.Models;

public sealed class TextPreviewResult
{
    public string? Text { get; init; }
    public string? ErrorMessage { get; init; }

    public bool Succeeded => !string.IsNullOrEmpty(Text);

    public static TextPreviewResult Ok(string text) => new() { Text = text };

    public static TextPreviewResult Failed(string message) => new() { ErrorMessage = message };
}
