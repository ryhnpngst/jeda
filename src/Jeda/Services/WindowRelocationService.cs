using System.Diagnostics;
using System.Drawing;
using System.Windows.Threading;
using Jeda.Models;
using Jeda.Native;

namespace Jeda.Services;

/// <summary>
/// Ensures application windows (e.g. Chrome, PowerPoint, Notes) are accessible on the primary display
/// and never trapped behind the topmost freeze overlay on the secondary display (FR-04, NFR-02).
/// </summary>
public sealed class WindowRelocationService : IDisposable
{
    private DispatcherTimer? _relocatorTimer;
    private MonitorInfo? _targetMonitor;
    private MonitorInfo? _primaryMonitor;
    private IntPtr _overlayHwnd;
    private bool _disposed;

    /// <summary>
    /// Begins monitoring foreground windows while freeze is active.
    /// If an application window appears on the frozen external monitor, it is immediately brought to the primary monitor.
    /// </summary>
    public void StartMonitoring(MonitorInfo targetMonitor, MonitorInfo primaryMonitor, IntPtr overlayHwnd)
    {
        _targetMonitor = targetMonitor;
        _primaryMonitor = primaryMonitor;
        _overlayHwnd = overlayHwnd;

        // 1. Move any existing open windows on the external monitor to primary monitor immediately
        MoveWindowsToPrimary();

        // 2. Start lightweight timer (every 250ms) to catch newly launched apps like Chrome
        _relocatorTimer?.Stop();
        _relocatorTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _relocatorTimer.Tick += (_, _) => CheckAndRelocateForeground();
        _relocatorTimer.Start();
    }

    /// <summary>
    /// Stops monitoring when freeze is resumed or closed.
    /// </summary>
    public void StopMonitoring()
    {
        _relocatorTimer?.Stop();
        _relocatorTimer = null;
    }

    private void MoveWindowsToPrimary()
    {
        if (_targetMonitor == null || _primaryMonitor == null) return;

        try
        {
            NativeMethods.EnumWindows((hwnd, _) =>
            {
                if (hwnd == _overlayHwnd || !NativeMethods.IsWindowVisible(hwnd)) return true;
                if (NativeMethods.GetWindowTextLength(hwnd) == 0) return true;

                if (NativeMethods.GetWindowRect(hwnd, out var rect))
                {
                    int centerX = (rect.Left + rect.Right) / 2;
                    int centerY = (rect.Top + rect.Bottom) / 2;

                    if (_targetMonitor.Bounds.Contains(centerX, centerY))
                    {
                        RelocateWindow(hwnd, rect);
                    }
                }
                return true;
            }, IntPtr.Zero);
        }
        catch
        {
            // Ignore transient Win32 errors
        }
    }

    private void CheckAndRelocateForeground()
    {
        if (_targetMonitor == null || _primaryMonitor == null) return;

        try
        {
            IntPtr fg = NativeMethods.GetForegroundWindow();
            if (fg == IntPtr.Zero || fg == _overlayHwnd) return;
            if (!NativeMethods.IsWindowVisible(fg)) return;
            if (NativeMethods.GetWindowTextLength(fg) == 0) return;

            if (NativeMethods.GetWindowRect(fg, out var rect))
            {
                int centerX = (rect.Left + rect.Right) / 2;
                int centerY = (rect.Top + rect.Bottom) / 2;

                if (_targetMonitor.Bounds.Contains(centerX, centerY))
                {
                    RelocateWindow(fg, rect);
                }
            }
        }
        catch
        {
            // Ignore transient Win32 errors
        }
    }

    private void RelocateWindow(IntPtr hwnd, RECT rect)
    {
        if (_primaryMonitor == null) return;

        int width = Math.Min(rect.Right - rect.Left, _primaryMonitor.Width - 100);
        int height = Math.Min(rect.Bottom - rect.Top, _primaryMonitor.Height - 100);
        if (width <= 200) width = 1200;
        if (height <= 200) height = 800;

        int targetX = _primaryMonitor.X + 60;
        int targetY = _primaryMonitor.Y + 60;

        NativeMethods.SetWindowPos(
            hwnd,
            IntPtr.Zero,
            targetX,
            targetY,
            width,
            height,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_SHOWWINDOW);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopMonitoring();
    }
}

