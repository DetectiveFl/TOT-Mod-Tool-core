using OutlastTrialsMod.Localization;

namespace OutlastTrialsMod.Config;

public static class AppState
{
    public static string? GameDirectory { get; set; }
    public static AppLanguage Language { get; set; } = AppLanguage.English;
}
