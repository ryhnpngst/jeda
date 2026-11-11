using System.Windows.Media.Imaging;
using Jeda.Models;

namespace Jeda.Services.Interfaces;

/// <summary>
/// Service interface for capturing monitor screens prior to freezing (FR-02).
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>
    /// Captures the specified monitor's bounds into an in-memory BitmapSource.
    /// Does not write files to disk (NFR-05 Privacy).
    /// </summary>
    BitmapSource CaptureScreen(MonitorInfo monitor);
}

