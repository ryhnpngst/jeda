using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Jeda.Models;
using Jeda.Native;

namespace Jeda.Views;

/// <summary>
/// Borderless fullscreen overlay window rendering a static screenshot on the selected monitor.
/// </summary>
public partial class FreezeOverlayWindow : Window
{
    public FreezeOverlayWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Displays the frozen screenshot positioned over the target monitor without stealing input focus.
    /// </summary>
    public void ShowOnMonitor(MonitorInfo targetMonitor, BitmapSource screenshot)
    {
        OverlayImage.Source = screenshot;

        // Position overlay to cover target monitor bounds
        Left = targetMonitor.X;
        Top = targetMonitor.Y;
        Width = targetMonitor.Width;
        Height = targetMonitor.Height;

        Show();

        // Ensure topmost z-order via Win32 API without activating or taking focus away from user
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(
            hwnd,
            new IntPtr(NativeMethods.HWND_TOPMOST),
            targetMonitor.X,
            targetMonitor.Y,
            targetMonitor.Width,
            targetMonitor.Height,
            NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_NOACTIVATE);
    }
}

