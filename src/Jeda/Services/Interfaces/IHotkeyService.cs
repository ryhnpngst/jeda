using System.Windows.Input;
using Jeda.Native;

namespace Jeda.Services.Interfaces;

/// <summary>
/// Service interface for registering and listening to system-wide global hotkeys (FR-05).
/// </summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>
    /// Registers a global hotkey with the specified modifiers and virtual key.
    /// </summary>
    bool RegisterHotKey(HotKeyModifiers modifiers, Key key, Action callback);

    /// <summary>
    /// Unregisters all registered hotkeys.
    /// </summary>
    void UnregisterAll();
}

