using System.Windows;
using System.Windows.Threading;
using Jeda.Native;

namespace Jeda;

/// <summary>
/// Application entry point and lifecycle manager for JEDA.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Enforce PerMonitorV2 DPI awareness before any window/screen API runs
        try
        {
            NativeMethods.SetProcessDpiAwarenessContext(NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        }
        catch
        {
            // Ignore if OS does not support or already set by manifest
        }

        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Terjadi kesalahan pada aplikasi JEDA:\n\n{e.Exception.Message}",
            "JEDA – Kesalahan Sistem",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show(
                $"Kesalahan kritis:\n\n{ex.Message}",
                "JEDA – Kesalahan Kritis",
                MessageBoxButton.OK,
                MessageBoxImage.Stop);
        }
    }
}
