using System.Runtime.InteropServices;

namespace Jeda.Native;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

/// <summary>
/// Win32 API interop declarations for display management, window overlay, hotkeys, and display topology.
/// </summary>
public static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;

    public const int HWND_TOPMOST = -1;
    public const int HWND_NOTOPMOST = -2;
    public const int HWND_TOP = 0;
    public const int HWND_BOTTOM = 1;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_TOPMOST = 0x00000008;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TRANSPARENT = 0x00000020;

    // DisplayConfig Constants
    public const uint QDC_DATABASE_CURRENT = 0x00000004;
    public const uint SDC_APPLY = 0x00000080;
    public const uint SDC_TOPOLOGY_INTERNAL = 0x00000001;
    public const uint SDC_TOPOLOGY_CLONE = 0x00000002;
    public const uint SDC_TOPOLOGY_EXTEND = 0x00000004;
    public const uint SDC_TOPOLOGY_EXTERNAL = 0x00000008;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
    public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    public static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    public static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    public static extern QueryDisplayConfigResult QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [Out] byte[] pathArray,
        ref uint numModeInfoArrayElements,
        [Out] byte[] modeInfoArray,
        out uint currentTopologyId);

    [DllImport("user32.dll")]
    public static extern int SetDisplayConfig(
        uint numPathArrayElements,
        IntPtr pathArray,
        uint numModeInfoArrayElements,
        IntPtr modeInfoArray,
        uint flags);
}

public enum QueryDisplayConfigResult
{
    Success = 0,
    InvalidParameter = 87,
    NotSupported = 50,
    AccessDenied = 5,
    GenFailure = 31
}
