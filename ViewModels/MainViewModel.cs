using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using OutlastTrialsMod.Config;
using App = OutlastTrialsMod.App;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Localization;
using OutlastTrialsMod.Mvvm;
using OutlastTrialsMod.Services;
using OutlastTrialsMod.Views;

namespace OutlastTrialsMod.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly RelayCommand _createModCommand;
    private readonly RelayCommand _changeDirectoryCommand;
    private readonly RelayCommand _settingsCommand;
    private readonly ModBuildService _modBuildService = new();
    private string _gameDirectory = string.Empty;
    private string _statusMessage;
    private bool _isBusy;
    private int _selectedTabIndex;

    public MainViewModel()
    {
        BrowserTab = new BrowserTabViewModel();
        ModTab = new ModTabViewModel();
        BrowserTab.PropertyChanged += OnBrowserTabPropertyChanged;
        ModTab.PropertyChanged += OnBrowserTabPropertyChanged;
        BrowserTab.ModTabRefreshCallback = async () =>
        {
            await ModTab.LoadTreeAsync().ConfigureAwait(true);
            NotifyCreateModCanExecuteChanged();
        };
        ModTab.ModifiedFilesChangedCallback = NotifyCreateModCanExecuteChanged;

        _createModCommand = new RelayCommand(() => _ = CreateModAsync(), CanCreateMod);
        _changeDirectoryCommand = new RelayCommand(() => _ = ChangeDirectoryAsync(), () => !IsBusy);
        _settingsCommand = new RelayCommand(OpenSettings, () => !IsBusy);

        GameDirectory = AppState.GameDirectory ?? string.Empty;
        _statusMessage = LocalizationManager.Instance.Ready;

        LocalizationManager.Instance.PropertyChanged += (_, e) =>
        {
            if (!IsBusy && e.PropertyName == nameof(LocalizationManager.Ready))
                StatusMessage = LocalizationManager.Instance.Ready;
        };
    }

    public BrowserTabViewModel BrowserTab { get; }
    public ModTabViewModel ModTab { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (!SetProperty(ref _selectedTabIndex, value))
                return;

            OnPropertyChanged(nameof(SearchQuery));
        }
    }

    public string SearchQuery
    {
        get => ActiveBrowserTab.SearchQuery;
        set => ActiveBrowserTab.SearchQuery = value;
    }

    private FileBrowserViewModel ActiveBrowserTab =>
        SelectedTabIndex == 1 ? ModTab : BrowserTab;

    private void OnBrowserTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileBrowserViewModel.SearchQuery) && sender == ActiveBrowserTab)
            OnPropertyChanged(nameof(SearchQuery));
    }

    public string GameDirectory
    {
        get => _gameDirectory;
        set
        {
            if (!SetProperty(ref _gameDirectory, value))
                return;

            _createModCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (!SetProperty(ref _isBusy, value))
                return;

            RefreshCommandStates();
        }
    }

    private void RefreshCommandStates()
    {
        _createModCommand.RaiseCanExecuteChanged();
        _changeDirectoryCommand.RaiseCanExecuteChanged();
        _settingsCommand.RaiseCanExecuteChanged();
    }

    public ICommand CreateModCommand => _createModCommand;
    public ICommand ChangeDirectoryCommand => _changeDirectoryCommand;
    public ICommand SettingsCommand => _settingsCommand;

    public async Task InitializeProviderAsync()
    {
        if (string.IsNullOrWhiteSpace(GameDirectory) || !Directory.Exists(GameDirectory))
            throw new DirectoryNotFoundException("Game directory is not set or does not exist.");

        IsBusy = true;
        StatusMessage = "Initializing CUE4Parse...";

        try
        {
            BrowserTab.CancelPendingWork();
            ModTab.CancelPendingWork();
            Cue4ParseService.Instance.DisposeProvider();

            var progress = new Progress<string>(msg => StatusMessage = msg);
            await Cue4ParseService.Instance.InitializeAsync(GameDirectory, progress).ConfigureAwait(true);
            StatusMessage = "Loading file browser...";
            await BrowserTab.LoadTreeAsync().ConfigureAwait(true);
            await ModTab.LoadTreeAsync().ConfigureAwait(true);
            StatusMessage = LocalizationManager.Instance.Ready;
            NotifyCreateModCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Initialization failed: {ex.Message}";
            MessageBox.Show(
                App.FormatExceptionMessage(ex),
                "Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ChangeDirectoryAsync()
    {
        if (IsBusy)
            return;

        var dialog = new OpenFolderDialog
        {
            Title = "Select the Outlast Trials game directory (folder containing .pak / .utoc files)",
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(GameDirectory) && Directory.Exists(GameDirectory))
            dialog.InitialDirectory = GameDirectory;

        if (dialog.ShowDialog() != true)
            return;

        var selectedPath = dialog.FolderName;
        if (string.IsNullOrWhiteSpace(selectedPath) || !Directory.Exists(selectedPath))
            return;

        AppState.GameDirectory = selectedPath;
        GameDirectory = selectedPath;
        await InitializeProviderAsync().ConfigureAwait(true);
    }

    private static void OpenSettings()
    {
        var owner = Application.Current.MainWindow;
        var settings = new SettingsWindow
        {
            Owner = owner
        };
        settings.ShowDialog();
    }

    private bool CanCreateMod() =>
        !IsBusy &&
        ModStagingPaths.HasModifiedFiles() &&
        !string.IsNullOrWhiteSpace(GameDirectory) &&
        Directory.Exists(GameDirectory);

    private void NotifyCreateModCanExecuteChanged() =>
        _createModCommand.RaiseCanExecuteChanged();

    private async Task CreateModAsync()
    {
        if (!CanCreateMod())
            return;

        var dialog = new ModNameWindow
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true)
            return;

        var modName = SanitizeModName(dialog.ViewModel.ModName.Trim());
        if (string.IsNullOrWhiteSpace(modName))
        {
            var loc = LocalizationManager.Instance;
            MessageBox.Show(
                loc.ModNameEmpty,
                loc.CreateMod,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var buildSucceeded = false;
        IsBusy = true;
        StatusMessage = LocalizationManager.Instance.CreatingMod;
        ModTab.ReleaseUiFileHandles();

        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            var result = await _modBuildService
                .BuildModAsync(modName, GameDirectory, progress)
                .ConfigureAwait(true);

            if (result is null)
            {
                StatusMessage = LocalizationManager.Instance.ModBuildFailed;
                return;
            }

            buildSucceeded = true;
            await ModTab.LoadTreeAsync().ConfigureAwait(true);
            StatusMessage = LocalizationManager.Instance.Ready;
        }
        catch (Exception ex)
        {
            var loc = LocalizationManager.Instance;
            MessageBox.Show(
                ex.Message,
                loc.CreateMod,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            StatusMessage = loc.Format(nameof(LocalizationManager.ModBuildError), ex.Message);
        }
        finally
        {
            IsBusy = false;

            if (!buildSucceeded && StatusMessage.StartsWith(LocalizationManager.Instance.PackingMod, StringComparison.Ordinal))
                StatusMessage = LocalizationManager.Instance.ModBuildFailed;

            RefreshCommandStates();
            NotifyCreateModCanExecuteChanged();
        }
    }

    private static string SanitizeModName(string modName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(modName
            .Where(ch => !invalidChars.Contains(ch))
            .ToArray())
            .Trim();

        return string.IsNullOrWhiteSpace(sanitized) ? string.Empty : sanitized;
    }
}
