using System.Drawing;
using System.Drawing.Drawing2D;
using Jeda.Native;
using Forms = System.Windows.Forms;

namespace Jeda.Services;

/// <summary>
/// Manages Windows System Tray (NotifyIcon) integration, tray context menus, and notification state (FR-07).
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _showMenuItem;
    private readonly Forms.ToolStripMenuItem _toggleFreezeMenuItem;
    private readonly Forms.ToolStripMenuItem _exitMenuItem;

    private readonly Icon _readyIcon;
    private readonly Icon _frozenIcon;

    private readonly Action _onShowRequested;
    private readonly Action _onToggleFreezeRequested;
    private readonly Action _onExitRequested;

    private bool _isFrozen;
    private bool _hasShownFirstBalloon;
    private bool _disposed;

    public TrayService(
        Action onShowRequested,
        Action onToggleFreezeRequested,
        Action onExitRequested)
    {
        _onShowRequested = onShowRequested ?? throw new ArgumentNullException(nameof(onShowRequested));
        _onToggleFreezeRequested = onToggleFreezeRequested ?? throw new ArgumentNullException(nameof(onToggleFreezeRequested));
        _onExitRequested = onExitRequested ?? throw new ArgumentNullException(nameof(onExitRequested));

        _readyIcon = CreatePlayIcon(Color.FromArgb(37, 99, 235));   // #2563EB (Biru - Resume / Live)
        _frozenIcon = CreatePauseIcon(Color.FromArgb(239, 68, 68)); // #EF4444 (Merah - Pause / Freeze)

        var contextMenu = new Forms.ContextMenuStrip();

        _showMenuItem = new Forms.ToolStripMenuItem("Tampilkan JEDA", null, (_, _) => _onShowRequested())
        {
            Font = new System.Drawing.Font(contextMenu.Font, System.Drawing.FontStyle.Bold)
        };

        _toggleFreezeMenuItem = new Forms.ToolStripMenuItem("Bekukan Layar (Freeze)", null, (_, _) => _onToggleFreezeRequested());

        _exitMenuItem = new Forms.ToolStripMenuItem("Keluar", null, (_, _) => _onExitRequested());

        contextMenu.Items.Add(_showMenuItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(_toggleFreezeMenuItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(_exitMenuItem);

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _readyIcon,
            Text = "JEDA – Siap (Ctrl + Shift + Z)",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => _onShowRequested();
    }

    /// <summary>
    /// Updates tray icon, tooltip, and menu state when freeze state changes.
    /// </summary>
    public void UpdateState(bool isFrozen, string? monitorTitle = null)
    {
        if (_disposed) return;

        _isFrozen = isFrozen;

        if (isFrozen)
        {
            _notifyIcon.Icon = _frozenIcon;
            string tip = string.IsNullOrWhiteSpace(monitorTitle)
                ? "JEDA – Layar Dibekukan (Ctrl + Shift + Z)"
                : $"JEDA – {monitorTitle} Dibekukan (Ctrl + Shift + Z)";
            _notifyIcon.Text = TruncateTip(tip);
            _toggleFreezeMenuItem.Text = "Lanjutkan Layar (Resume)";
        }
        else
        {
            _notifyIcon.Icon = _readyIcon;
            _notifyIcon.Text = TruncateTip("JEDA – Siap (Ctrl + Shift + Z)");
            _toggleFreezeMenuItem.Text = "Bekukan Layar (Freeze)";
        }
    }

    private static string TruncateTip(string text)
    {
        return text.Length > 63 ? text[..60] + "..." : text;
    }

    /// <summary>
    /// Shows a balloon notification when JEDA is minimized to the system tray.
    /// </summary>
    public void ShowMinimizeBalloonTip()
    {
        if (_disposed) return;

        if (!_hasShownFirstBalloon)
        {
            _hasShownFirstBalloon = true;
            _notifyIcon.ShowBalloonTip(
                2500,
                "JEDA Berjalan di Latar Belakang",
                "Tekan Ctrl + Shift + Z kapan saja untuk Freeze / Resume layar eksternal.",
                Forms.ToolTipIcon.Info);
        }
    }

    private static Icon CreatePlayIcon(Color bgColor)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background circle
            using var bgBrush = new SolidBrush(bgColor);
            g.FillEllipse(bgBrush, 2, 2, 28, 28);

            // Play / Resume triangle pointing right in white
            using var whiteBrush = new SolidBrush(Color.White);
            var points = new[]
            {
                new PointF(12, 9),
                new PointF(12, 23),
                new PointF(23, 16)
            };
            g.FillPolygon(whiteBrush, points);
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var tempIcon = Icon.FromHandle(hIcon);
            return (Icon)tempIcon.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(hIcon);
        }
    }

    private static Icon CreatePauseIcon(Color bgColor)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background circle
            using var bgBrush = new SolidBrush(bgColor);
            g.FillEllipse(bgBrush, 2, 2, 28, 28);

            // Pause indicator bars in white
            using var whiteBrush = new SolidBrush(Color.White);
            g.FillRectangle(whiteBrush, 10, 9, 4, 14);
            g.FillRectangle(whiteBrush, 18, 9, 4, 14);
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var tempIcon = Icon.FromHandle(hIcon);
            return (Icon)tempIcon.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(hIcon);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _readyIcon.Dispose();
        _frozenIcon.Dispose();
    }
}
