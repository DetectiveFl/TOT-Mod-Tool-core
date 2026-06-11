using System.Collections.ObjectModel;
using System.ComponentModel;
using OutlastTrialsMod.Localization;
using OutlastTrialsMod.Mvvm;

namespace OutlastTrialsMod.ViewModels;

public sealed class LanguageOption
{
    public LanguageOption(AppLanguage language, string displayName)
    {
        Language = language;
        DisplayName = displayName;
    }

    public AppLanguage Language { get; }
    public string DisplayName { get; }
}

public sealed class SettingsViewModel : ViewModelBase
{
    private LanguageOption? _selectedLanguage;

    public SettingsViewModel()
    {
        Languages = new ObservableCollection<LanguageOption>();
        RefreshLanguageOptions();
        LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
    }

    public ObservableCollection<LanguageOption> Languages { get; }

    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (!SetProperty(ref _selectedLanguage, value) || value is null)
                return;

            LocalizationManager.Instance.SetLanguage(value.Language);
        }
    }

    public string WindowTitle => LocalizationManager.Instance.Settings;
    public string LanguageLabel => LocalizationManager.Instance.Language;

    private void RefreshLanguageOptions()
    {
        var loc = LocalizationManager.Instance;
        var currentLanguage = _selectedLanguage?.Language ?? loc.CurrentLanguage;

        Languages.Clear();
        Languages.Add(new LanguageOption(AppLanguage.English, loc.English));
        Languages.Add(new LanguageOption(AppLanguage.Russian, loc.Russian));
        Languages.Add(new LanguageOption(AppLanguage.Chinese, loc.Chinese));

        _selectedLanguage = Languages.First(l => l.Language == currentLanguage);
        OnPropertyChanged(nameof(SelectedLanguage));
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LocalizationManager.English) or nameof(LocalizationManager.Russian))
            RefreshLanguageOptions();

        if (e.PropertyName is nameof(LocalizationManager.Settings) or nameof(LocalizationManager.Language))
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(LanguageLabel));
        }
    }
}
