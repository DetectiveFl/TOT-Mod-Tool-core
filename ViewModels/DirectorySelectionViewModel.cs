using System.IO;
using Microsoft.Win32;
using OutlastTrialsMod.Config;
using OutlastTrialsMod.Mvvm;

namespace OutlastTrialsMod.ViewModels;

public sealed class DirectorySelectionViewModel : ViewModelBase
{
    private string _gameDirectory = string.Empty;

    public string GameDirectory
    {
        get => _gameDirectory;
        set
        {
            if (!SetProperty(ref _gameDirectory, value)) return;
            OkCommand.RaiseCanExecuteChanged();
        }
    }

    public RelayCommand BrowseCommand { get; }
    public RelayCommand OkCommand { get; }

    public event EventHandler? DirectoryConfirmed;

    public DirectorySelectionViewModel()
    {
        if (!string.IsNullOrEmpty(AppState.GameDirectory))
            GameDirectory = AppState.GameDirectory;

        BrowseCommand = new RelayCommand(Browse);
        OkCommand = new RelayCommand(ConfirmOk, () => Directory.Exists(GameDirectory));
    }

    private void Browse()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select the Outlast Trials game directory (folder containing .pak / .utoc files)",
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(GameDirectory) && Directory.Exists(GameDirectory))
            dialog.InitialDirectory = GameDirectory;

        if (dialog.ShowDialog() == true)
            GameDirectory = dialog.FolderName;
    }

    private void ConfirmOk()
    {
        if (!Directory.Exists(GameDirectory)) return;

        AppState.GameDirectory = GameDirectory;
        DirectoryConfirmed?.Invoke(this, EventArgs.Empty);
    }
}
