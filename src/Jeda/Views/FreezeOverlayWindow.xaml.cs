using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Jeda.Models;
using Jeda.Native;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Jeda.Views;

/// <summary>
/// Fullscreen borderless overlay on the external monitor providing Live Mirroring and Freeze capability (FR-00, FR-03, FR-04).
/// </summary>
public partial class FreezeOverlayWindow : Window
{
    private WriteableBitmap? _writeableBitmap;
    private DispatcherTimer? _mirrorTimer;
    private MonitorInfo? _primaryMonitor;
    private MonitorInfo? _targetMonitor;
    private bool _isLive;

    public FreezeOverlayWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        int exStyle = NativeMethods.GetWindowLong32(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong32(
            hwnd,
            NativeMethods.GWL_EXSTYLE,
            exStyle | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_TOPMOST | NativeMethods.WS_EX_NOACTIVATE);
    }

    /// <summary>
    /// Starts live mirroring of the primary monitor onto this external display.
    /// </summary>
    public void StartLiveMirror(MonitorInfo primaryMonitor, MonitorInfo targetMonitor)
    {
        _primaryMonitor = primaryMonitor;
        _targetMonitor = targetMonitor;

        Left = targetMonitor.X;
        Top = targetMonitor.Y;
        Width = targetMonitor.Width;
        Height = targetMonitor.Height;

        _writeableBitmap = new WriteableBitmap(
            primaryMonitor.Width,
            primaryMonitor.Height,
            96,
            96,
            System.Windows.Media.PixelFormats.Bgr32,
            null);

        OverlayImage.Source = _writeableBitmap;

        Show();

        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(
            hwnd,
            new IntPtr(NativeMethods.HWND_TOPMOST),
            targetMonitor.X,
            targetMonitor.Y,
            targetMonitor.Width,
            targetMonitor.Height,
            NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_NOACTIVATE);

        CaptureFrame();

        _isLive = true;
        _mirrorTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        _mirrorTimer.Tick += (_, _) =>
        {
            if (_isLive)
            {
                CaptureFrame();
            }
        };
        _mirrorTimer.Start();
    }

    /// <summary>
    /// Freezes the display by pausing the live stream on the current frame (FR-03).
    /// </summary>
    public void Freeze()
    {
        _isLive = false;
        CaptureFrame();
    }

    /// <summary>
    /// Resumes the live stream from the primary monitor (FR-04).
    /// </summary>
    public void Resume()
    {
        _isLive = true;
    }

    private void CaptureFrame()
    {
        if (_writeableBitmap == null || _primaryMonitor == null) return;

        _writeableBitmap.Lock();
        try
        {
            using var bmp = new Bitmap(
                _writeableBitmap.PixelWidth,
                _writeableBitmap.PixelHeight,
                _writeableBitmap.BackBufferStride,
                PixelFormat.Format32bppRgb,
                _writeableBitmap.BackBuffer);

            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(
                _primaryMonitor.X,
                _primaryMonitor.Y,
                0,
                0,
                _primaryMonitor.Bounds.Size,
                CopyPixelOperation.SourceCopy);

            _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight));
        }
        catch
        {
            // Ignore transient errors during resolution switches
        }
        finally
        {
            _writeableBitmap.Unlock();
        }
    }

    /// <summary>
    /// Shuts down the overlay and disposes all bitmap resources.
    /// </summary>
    public void CloseOverlay()
    {
        _isLive = false;
        _mirrorTimer?.Stop();
        _mirrorTimer = null;
        OverlayImage.Source = null;
        _writeableBitmap = null;
        Close();
    }
}
