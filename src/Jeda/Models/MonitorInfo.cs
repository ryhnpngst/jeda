using System.Drawing;

namespace Jeda.Models;

/// <summary>
/// Represents metadata and geometry of a connected display monitor.
/// </summary>
public sealed class MonitorInfo
{
    public required int Index { get; init; }
    public required string DeviceName { get; init; }
    public required string FriendlyName { get; init; }
    public required Rectangle Bounds { get; init; }
    public required Rectangle WorkingArea { get; init; }
    public required bool IsPrimary { get; init; }

    public int X => Bounds.X;
    public int Y => Bounds.Y;
    public int Width => Bounds.Width;
    public int Height => Bounds.Height;
    public string Resolution => $"{Width} × {Height}";

    public string DisplayTitle => IsPrimary
        ? $"Monitor {Index} [Utama] – {Resolution}"
        : $"Monitor {Index} [Eksternal] – {Resolution}";

    public override string ToString() => DisplayTitle;
}

