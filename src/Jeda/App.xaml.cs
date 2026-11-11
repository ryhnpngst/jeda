using System.Windows;
using System.Windows.Threading;

namespace Jeda;

/// <summary>
/// Application entry point and lifecycle manager for JEDA.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
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
