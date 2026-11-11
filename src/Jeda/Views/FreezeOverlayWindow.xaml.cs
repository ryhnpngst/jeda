using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Jeda.Models;
using Jeda.Native;
using Jeda.Utilities;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Jeda.Views;

/// <summary>
/// Fullscreen borderless overlay on the external monitor providing HD Live Mirroring and Freeze capability (FR-00, FR-03, FR-04).
/// Completely transparent to mouse input (WS_EX_TRANSPARENT) so other applications (like Chrome) are never blocked.
/// </summary>
public partial class FreezeOverlayWindow : Window
{
    private WriteableBitmap? _writeableBitmap;
    private MonitorInfo? _primaryMonitor;
    private MonitorInfo? _targetMonitor;
    private CancellationTokenSource? _cts;
    private Task? _mirrorTask;
    private readonly object _stateLock = new();
    private bool _isLive;
    private IntPtr _hwnd;

    public IntPtr Handle => _hwnd;

    public FreezeOverlayWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwnd = new WindowInteropHelper(this).Handle;
        int exStyle = NativeMethods.GetWindowLong32(_hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong32(
            _hwnd,
            NativeMethods.GWL_EXSTYLE,
            exStyle | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TRANSPARENT);
    }

    /// <summary>
    /// Starts live mirroring of the primary monitor onto this external display.
    /// </summary>
    public void StartLiveMirror(MonitorInfo primaryMonitor, MonitorInfo targetMonitor)
    {
        _primaryMonitor = primaryMonitor;
        _targetMonitor = targetMonitor;

        PositionToMonitor(targetMonitor);

        _writeableBitmap = new WriteableBitmap(
            primaryMonitor.Width,
            primaryMonitor.Height,
            96,
            96,
            System.Windows.Media.PixelFormats.Bgr32,
            null);

        OverlayImage.Source = _writeableBitmap;

        Show();

        // During Live Mirroring: keep at normal z-order (HWND_TOP), not topmost
        _hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(
            _hwnd,
            new IntPtr(NativeMethods.HWND_TOP),
            targetMonitor.X,
            targetMonitor.Y,
            targetMonitor.Width,
            targetMonitor.Height,
            NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_NOACTIVATE);

        CaptureDirectToBitmap();

        _isLive = true;
        _cts = new CancellationTokenSource();
        _mirrorTask = Task.Run(() => MirrorWorkerLoop(_cts.Token));

        // Trim working set shortly after startup initialization
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            MemoryOptimizer.TrimWorkingSet();
        });
    }

    /// <summary>
    /// Freezes the display on the current frame and makes the window Topmost (FR-03).
    /// </summary>
    public void Freeze()
    {
        lock (_stateLock)
        {
            _isLive = false;
        }

        CaptureDirectToBitmap();

        if (_hwnd != IntPtr.Zero && _targetMonitor != null)
        {
            NativeMethods.SetWindowPos(
                _hwnd,
                new IntPtr(NativeMethods.HWND_TOPMOST),
                _targetMonitor.X,
                _targetMonitor.Y,
                _targetMonitor.Width,
                _targetMonitor.Height,
                NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_NOACTIVATE);
        }

        // Flush memory immediately upon freeze since the background mirror loop is paused
        MemoryOptimizer.TrimWorkingSet();
    }

    /// <summary>
    /// Resumes live stream: removes Topmost and resumes background capture (FR-04).
    /// </summary>
    public void Resume()
    {
        if (_hwnd != IntPtr.Zero && _targetMonitor != null)
        {
            NativeMethods.SetWindowPos(
                _hwnd,
                new IntPtr(NativeMethods.HWND_NOTOPMOST),
                _targetMonitor.X,
                _targetMonitor.Y,
                _targetMonitor.Width,
                _targetMonitor.Height,
                NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_NOACTIVATE);
        }

        lock (_stateLock)
        {
            _isLive = true;
        }
    }

    private void PositionToMonitor(MonitorInfo monitor)
    {
        double dpiX = 1.0;
        double dpiY = 1.0;

        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            dpiX = source.CompositionTarget.TransformToDevice.M11;
            dpiY = source.CompositionTarget.TransformToDevice.M22;
        }

        if (dpiX <= 0) dpiX = 1.0;
        if (dpiY <= 0) dpiY = 1.0;

        Left = monitor.X / dpiX;
        Top = monitor.Y / dpiY;
        Width = monitor.Width / dpiX;
        Height = monitor.Height / dpiY;
    }

    private async Task MirrorWorkerLoop(CancellationToken token)
    {
        int width = _primaryMonitor!.Width;
        int height = _primaryMonitor!.Height;

        using var memoryBmp = new Bitmap(width, height, PixelFormat.Format32bppRgb);
        using var g = Graphics.FromImage(memoryBmp);

        var stopwatch = new System.Diagnostics.Stopwatch();

        while (!token.IsCancellationRequested)
        {
            bool shouldCapture;
            lock (_stateLock)
            {
                shouldCapture = _isLive;
            }

            if (shouldCapture)
            {
                stopwatch.Restart();
                try
                {
                    g.CopyFromScreen(
                        _primaryMonitor.X,
                        _primaryMonitor.Y,
                        0,
                        0,
                        _primaryMonitor.Bounds.Size,
                        CopyPixelOperation.SourceCopy);

                    var data = memoryBmp.LockBits(
                        new Rectangle(0, 0, width, height),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppRgb);

                    try
                    {
                        var dispatcher = Dispatcher;
                        if (dispatcher != null && !token.IsCancellationRequested)
                        {
                            await dispatcher.InvokeAsync(() =>
                            {
                                if (_writeableBitmap == null || token.IsCancellationRequested) return;

                                _writeableBitmap.Lock();
                                try
                                {
                                    uint byteCount = (uint)(_writeableBitmap.BackBufferStride * height);
                                    NativeMethods.CopyMemory(_writeableBitmap.BackBuffer, data.Scan0, byteCount);
                                    _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
                                }
                                finally
                                {
                                    _writeableBitmap.Unlock();
                                }
                            }, System.Windows.Threading.DispatcherPriority.Render, token);
                        }
                    }
                    finally
                    {
                        memoryBmp.UnlockBits(data);
                    }
                }
                catch
                {
                    // Ignore transient errors
                }

                stopwatch.Stop();
                int elapsed = (int)stopwatch.ElapsedMilliseconds;
                int delay = Math.Max(15, 45 - elapsed);
                try
                {
                    await Task.Delay(delay, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            else
            {
                try
                {
                    await Task.Delay(100, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private void CaptureDirectToBitmap()
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
        lock (_stateLock)
        {
            _isLive = false;
        }

        try
        {
            _cts?.Cancel();
        }
        catch { }

        OverlayImage.Source = null;
        _writeableBitmap = null;
        Close();
    }
}
