using System.Drawing;
using Microsoft.Win32;
using Jeda.Models;
using Jeda.Services.Interfaces;
using Screen = System.Windows.Forms.Screen;

namespace Jeda.Services;

/// <summary>
/// Implements concrete monitor detection using System.Windows.Forms.Screen and system display events (FR-01, FR-06).
/// Automatically tracks display change events (NFR-02).
/// </summary>
public sealed class MonitorService : IMonitorService
{
    private bool _disposed;

    public event EventHandler? DisplayConfigurationChanged;

    public MonitorService()
    {
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    /// <summary>
    /// Retrieves all connected monitors, ordered with the primary display first.
    /// </summary>
    public IReadOnlyList<MonitorInfo> GetAllMonitors()
    {
        var screens = Screen.AllScreens;
        var result = new List<MonitorInfo>(screens.Length);

        var sortedScreens = screens
            .OrderByDescending(s => s.Primary)
            .ThenBy(s => s.DeviceName)
            .ToList();

        for (int i = 0; i < sortedScreens.Count; i++)
        {
            var screen = sortedScreens[i];
            int displayIndex = i + 1;
            string roleLabel = screen.Primary ? "Utama" : "Eksternal";
            string friendlyName = $"Monitor {displayIndex} ({roleLabel})";

            result.Add(new MonitorInfo
            {
                Index = displayIndex,
                DeviceName = screen.DeviceName,
                FriendlyName = friendlyName,
                Bounds = screen.Bounds,
                WorkingArea = screen.WorkingArea,
                IsPrimary = screen.Primary
            });
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Returns the primary monitor.
    /// </summary>
    public MonitorInfo? GetPrimaryMonitor()
    {
        var monitors = GetAllMonitors();
        return monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
    }

    /// <summary>
    /// Returns the first external monitor for presentation target; falls back to primary if only one display exists.
    /// </summary>
    public MonitorInfo? GetDefaultTargetMonitor()
    {
        var monitors = GetAllMonitors();
        return monitors.FirstOrDefault(m => !m.IsPrimary) ?? monitors.FirstOrDefault();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        DisplayConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }
}

