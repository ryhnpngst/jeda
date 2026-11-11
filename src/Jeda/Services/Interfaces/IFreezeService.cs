using Jeda.Models;

namespace Jeda.Services.Interfaces;

public sealed class FreezeStateChangedEventArgs : EventArgs
{
    public required bool IsFrozen { get; init; }
    public MonitorInfo? TargetMonitor { get; init; }
}

/// <summary>
/// Service interface for orchestrating Freeze and Resume operations (FR-03, FR-04).
/// </summary>
public interface IFreezeService
{
    /// <summary>
    /// Indicates whether a screen is currently frozen.
    /// </summary>
    bool IsFrozen { get; }

    /// <summary>
    /// The monitor that is currently frozen, or null if not frozen.
    /// </summary>
    MonitorInfo? CurrentFrozenMonitor { get; }

    /// <summary>
    /// Event fired when freeze status transitions between active and idle.
    /// </summary>
    event EventHandler<FreezeStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Freezes the target monitor by capturing its current display and overlaying it fullscreen.
    /// </summary>
    void Freeze(MonitorInfo targetMonitor);

    /// <summary>
    /// Resumes live display on the frozen monitor and disposes the overlay.
    /// </summary>
    void Resume();

    /// <summary>
    /// Toggles between Freeze and Resume.
    /// </summary>
    void Toggle(MonitorInfo? targetMonitor = null);
}

