using Jeda.Models;

namespace Jeda.Services.Interfaces;

/// <summary>
/// Service interface for detecting and managing connected monitors (FR-01, FR-06).
/// </summary>
public interface IMonitorService : IDisposable
{
    /// <summary>
    /// Returns all currently connected display monitors with resolution and position.
    /// </summary>
    IReadOnlyList<MonitorInfo> GetAllMonitors();

    /// <summary>
    /// Returns the primary display monitor.
    /// </summary>
    MonitorInfo? GetPrimaryMonitor();

    /// <summary>
    /// Returns the default external monitor if available; otherwise returns the primary monitor.
    /// </summary>
    MonitorInfo? GetDefaultTargetMonitor();

    /// <summary>
    /// Fired when connected displays change (plugged/unplugged/resolution changed).
    /// </summary>
    event EventHandler? DisplayConfigurationChanged;
}
