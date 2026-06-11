using CUE4Parse.FileProvider.Objects;
using OutlastTrialsMod.Services;

namespace OutlastTrialsMod.ViewModels;

public sealed class BrowserTabViewModel : FileBrowserViewModel
{
    public BrowserTabViewModel() : base(isModTab: false) { }

    protected override IEnumerable<GameFile> GetSourceFiles()
    {
        var provider = Cue4ParseService.Instance.Provider;
        return provider?.Files.Values ?? Enumerable.Empty<GameFile>();
    }
}
