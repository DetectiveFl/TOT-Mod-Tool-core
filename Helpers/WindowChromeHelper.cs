using System.Windows;
using System.Windows.Input;

namespace OutlastTrialsMod.Helpers;

public static class WindowChromeHelper
{
    public static void OnTitleBarMouseLeftButtonDown(Window window, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && window.ResizeMode == ResizeMode.CanResize)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
            window.DragMove();
    }

    public static void Minimize(Window window) =>
        window.WindowState = WindowState.Minimized;

    public static void ToggleMaximize(Window window)
    {
        if (window.ResizeMode == ResizeMode.NoResize)
            return;

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    public static void Close(Window window) =>
        window.Close();

    public static void CloseAndShutdown(Window window)
    {
        window.Close();
        if (System.Windows.Application.Current is not null)
            System.Windows.Application.Current.Shutdown();
    }
}
