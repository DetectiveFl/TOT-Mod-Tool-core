using OutlastTrialsMod.Mvvm;

namespace OutlastTrialsMod.ViewModels;

public sealed class ModNameViewModel : ViewModelBase
{
    private string _modName = string.Empty;

    public ModNameViewModel()
    {
        OkCommand = new RelayCommand(ConfirmOk, CanConfirmOk);
    }

    public string ModName
    {
        get => _modName;
        set
        {
            if (!SetProperty(ref _modName, value))
                return;

            OkCommand.RaiseCanExecuteChanged();
        }
    }

    public RelayCommand OkCommand { get; }

    public event EventHandler? Confirmed;

    private bool CanConfirmOk() => !string.IsNullOrWhiteSpace(ModName);

    private void ConfirmOk()
    {
        if (!CanConfirmOk())
            return;

        Confirmed?.Invoke(this, EventArgs.Empty);
    }
}
