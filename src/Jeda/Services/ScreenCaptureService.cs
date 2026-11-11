using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Jeda.Models;
using Jeda.Services.Interfaces;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Jeda.Services;

/// <summary>
/// High-performance, in-memory screen capture service using GDI+ and WPF BitmapSource (FR-02, NFR-01, NFR-05).
/// Captures exact aspect ratio and native resolution without writing any files to disk.
/// </summary>
public sealed class ScreenCaptureService : IScreenCaptureService
{
    /// <summary>
    /// Captures the specified monitor's screen bounds into an immutable, thread-safe BitmapSource.
    /// </summary>
    /// <param name="monitor">The target monitor to capture.</param>
    /// <returns>Frozen BitmapSource containing the static screen image.</returns>
    public BitmapSource CaptureScreen(MonitorInfo monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        if (monitor.Width <= 0 || monitor.Height <= 0)
        {
            throw new ArgumentException(
                $"Dimensi monitor tidak valid: {monitor.Width} × {monitor.Height}",
                nameof(monitor));
        }

        return CaptureBounds(monitor.Bounds);
    }

    /// <summary>
    /// Captures an arbitrary screen rectangle in virtual desktop coordinates.
    /// </summary>
    public BitmapSource CaptureBounds(Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentException(
                $"Batas area tangkapan layar tidak valid: {bounds.Width} × {bounds.Height}",
                nameof(bounds));
        }

        // 1. Allocate 32bpp ARGB bitmap matching exact target dimensions
        using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

        // 2. Perform GDI CopyFromScreen (SourceCopy uses SRCCOPY | CAPTUREBLT under the hood)
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                bounds.X,
                bounds.Y,
                0,
                0,
                bounds.Size,
                CopyPixelOperation.SourceCopy);
        }

        // 3. Convert GDI+ Bitmap to WPF BitmapSource using zero-leak LockBits buffer copy
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            var bitmapSource = BitmapSource.Create(
                bitmapData.Width,
                bitmapData.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);

            // 4. Freeze BitmapSource to make it cross-thread accessible, immutable, and performant (NFR-01)
            bitmapSource.Freeze();

            return bitmapSource;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }
}

