using System.Windows;
using System.Windows.Threading;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Views;

namespace OutlastTrialsMod;

public partial class App : System.Windows.Application
{
    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        NativeDecoderInitializer.Initialize();

        if (!ShowDirectorySelection(reopenMain: false))
            Shutdown();
    }

    public bool ShowDirectorySelection(bool reopenMain)
    {
        var dialog = new DirectorySelectionWindow();
        if (dialog.ShowDialog() != true)
        {
            if (reopenMain)
                Shutdown();
            return false;
        }

        OpenMainWindow();
        return true;
    }

    private void OpenMainWindow()
    {
        var main = new MainWindow();
        MainWindow = main;
        main.Closed += (_, _) =>
        {
            if (MainWindow == main)
                MainWindow = null;
        };
        main.Show();
        _ = InitializeMainWindowAsync(main);
    }

    private async Task InitializeMainWindowAsync(MainWindow main)
    {
        try
        {
            await main.ViewModel.InitializeProviderAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Initialization Error", ex);
            main.ViewModel.StatusMessage = $"Initialization failed: {ex.Message}";
        }
    }

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowErrorDialog("Unexpected Error", e.Exception);
        e.Handled = true;
    }

    internal static void ShowErrorDialog(string title, Exception ex)
    {
        var message = FormatExceptionMessage(ex);
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    internal static string FormatExceptionMessage(Exception ex)
    {
        if (ex.InnerException is null)
            return ex.Message;

        return $"{ex.Message}\n\nDetails: {ex.InnerException.Message}";
    }
}
