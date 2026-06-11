using OutlastTrialsMod.Mvvm;

namespace OutlastTrialsMod.Models;

public sealed class LocresEntryRow : ViewModelBase
{
    private string _value = string.Empty;

    public LocresEntryRow(string @namespace, string key, string value)
    {
        Namespace = @namespace;
        Key = key;
        _value = value;
    }

    public string Namespace { get; }

    public string Key { get; }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}
