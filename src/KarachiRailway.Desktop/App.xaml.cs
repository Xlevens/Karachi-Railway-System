using System.Windows;
using System.Windows.Threading;

namespace KarachiRailway.Desktop;

/// <summary>
/// Application entry point.  Registers a global exception handler so that
/// unhandled dispatcher exceptions show a friendly message instead of silently
/// crashing on startup or during simulation playback.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\n" +
            "The application will attempt to continue. " +
            "If the problem persists, please restart.",
            "Karachi Railway System – Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}

