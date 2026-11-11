using System.Windows.Input;
using System.Windows.Interop;
using Jeda.Native;
using Jeda.Services.Interfaces;

namespace Jeda.Services;

/// <summary>
/// Implements system-wide global hotkey registration and dispatching (FR-05).
/// Enables shortcut execution (Ctrl + Shift + Z) from any application.
/// </summary>
public sealed class HotkeyService : IHotkeyService
{
    private readonly HwndSource _source;
    private readonly IntPtr _hwnd;
    private readonly Dictionary<int, Action> _callbacks = new();
    private int _currentId = 9000;
    private bool _disposed;

    public HotkeyService(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd)
                  ?? throw new InvalidOperationException("Gagal mengaitkan HwndSource ke window handle.");
        _source.AddHook(HwndHook);
    }

    public bool RegisterHotKey(HotKeyModifiers modifiers, Key key, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        int id = Interlocked.Increment(ref _currentId);
        int vk = KeyInterop.VirtualKeyFromKey(key);

        bool success = NativeMethods.RegisterHotKey(_hwnd, id, (uint)modifiers, (uint)vk);
        if (success)
        {
            _callbacks[id] = callback;
            return true;
        }

        return false;
    }

    public void UnregisterAll()
    {
        foreach (var id in _callbacks.Keys)
        {
            NativeMethods.UnregisterHotKey(_hwnd, id);
        }
        _callbacks.Clear();
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_callbacks.TryGetValue(id, out var action))
            {
                action();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        UnregisterAll();
        _source.RemoveHook(HwndHook);
    }
}

