using System.Windows;
using Jeda.Models;
using Jeda.Services.Interfaces;
using Jeda.Utilities;
using Jeda.Views;

namespace Jeda.Services;

/// <summary>
/// Orchestrates display topology, live mirroring, window relocation, and freeze/resume operations (FR-00, FR-03, FR-04).
/// </summary>
public sealed class FreezeService : IFreezeService
{
    private readonly IMonitorService _monitorService;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IDisplayTopologyService _topologyService;
    private readonly WindowRelocationService _relocationService = new();
    private readonly object _lock = new();

    private FreezeOverlayWindow? _overlayWindow;
    private MonitorInfo? _currentFrozenMonitor;
    private bool _isFrozen;
    private bool _disposed;

    public FreezeService(
        IMonitorService monitorService,
        IScreenCaptureService screenCaptureService,
        IDisplayTopologyService topologyService)
    {
        _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
        _screenCaptureService = screenCaptureService ?? throw new ArgumentNullException(nameof(screenCaptureService));
        _topologyService = topologyService ?? throw new ArgumentNullException(nameof(topologyService));

        // 1. Initialize topology: If initially in Duplicate (Clone), auto-switch to Extend (FR-00)
        _topologyService.InitializeAtStartup();

        _monitorService.DisplayConfigurationChanged += OnDisplayConfigurationChanged;

        // 2. Start Live Mirroring if external display is available
        StartLiveMirroringIfAvailable();
    }

    public bool IsFrozen
    {
        get
        {
            lock (_lock)
            {
                return _isFrozen;
            }
        }
    }

    public MonitorInfo? CurrentFrozenMonitor
    {
        get
        {
            lock (_lock)
            {
                return _currentFrozenMonitor;
            }
        }
    }

    public event EventHandler<FreezeStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Freezes the target monitor by pausing live mirroring on the current frame (FR-03).
    /// Protects the primary display so applications (like Chrome) are never blocked.
    /// </summary>
    public void Freeze(MonitorInfo targetMonitor)
    {
        ArgumentNullException.ThrowIfNull(targetMonitor);

        lock (_lock)
        {
            if (_isFrozen) return;

            // Enforce presentation safety: Never freeze the primary laptop display
            var resolvedTarget = targetMonitor;
            if (resolvedTarget.IsPrimary)
            {
                var external = _monitorService.GetAllMonitors().FirstOrDefault(m => !m.IsPrimary);
                if (external != null)
                {
                    resolvedTarget = external;
                }
                else
                {
                    // If in Clone mode, ensure Extend mode is applied
                    _topologyService.SwitchToExtend();
                    System.Threading.Thread.Sleep(500);
                    external = _monitorService.GetAllMonitors().FirstOrDefault(m => !m.IsPrimary);
                    if (external != null)
                    {
                        resolvedTarget = external;
                    }
                }
            }

            var primary = _monitorService.GetPrimaryMonitor() ?? resolvedTarget;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => FreezeInternal(resolvedTarget));
            }
            else
            {
                FreezeInternal(resolvedTarget);
            }

            // Start window relocator: If Chrome or any app opens, ensure it appears on the primary laptop screen
            if (_overlayWindow != null)
            {
                _relocationService.StartMonitoring(resolvedTarget, primary, _overlayWindow.Handle);
            }

            _isFrozen = true;
            _currentFrozenMonitor = resolvedTarget;
        }

        OnStateChanged(true, _currentFrozenMonitor);
    }

    /// <summary>
    /// Resumes live mirroring to the external monitor (FR-04).
    /// </summary>
    public void Resume()
    {
        lock (_lock)
        {
            if (!_isFrozen) return;

            _relocationService.StopMonitoring();

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => _overlayWindow?.Resume());
            }
            else
            {
                _overlayWindow?.Resume();
            }

            _isFrozen = false;
            _currentFrozenMonitor = null;
        }

        OnStateChanged(false, null);
    }

    /// <summary>
    /// Toggles between Freeze and Resume state.
    /// </summary>
    public void Toggle(MonitorInfo? targetMonitor = null)
    {
        if (IsFrozen)
        {
            Resume();
        }
        else
        {
            var target = targetMonitor
                         ?? _monitorService.GetDefaultTargetMonitor()
                         ?? throw new InvalidOperationException("Tidak ada monitor yang tersedia untuk dibekukan.");

            Freeze(target);
        }
    }

    public void StartLiveMirroringIfAvailable()
    {
        var primary = _monitorService.GetPrimaryMonitor();
        var external = _monitorService.GetDefaultTargetMonitor();

        if (primary == null || external == null || primary.DeviceName == external.DeviceName)
        {
            return;
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => StartLiveMirrorInternal(primary, external));
        }
        else
        {
            StartLiveMirrorInternal(primary, external);
        }
    }

    private void StartLiveMirrorInternal(MonitorInfo primary, MonitorInfo external)
    {
        if (_overlayWindow == null)
        {
            _overlayWindow = new FreezeOverlayWindow();
            _overlayWindow.StartLiveMirror(primary, external);
        }
    }

    private void FreezeInternal(MonitorInfo targetMonitor)
    {
        if (_overlayWindow != null)
        {
            _overlayWindow.Freeze();
        }
        else
        {
            var primary = _monitorService.GetPrimaryMonitor() ?? targetMonitor;
            _overlayWindow = new FreezeOverlayWindow();
            _overlayWindow.StartLiveMirror(primary, targetMonitor);
            _overlayWindow.Freeze();
        }
    }

    private void CloseOverlay()
    {
        _relocationService.StopMonitoring();
        _overlayWindow?.CloseOverlay();
        _overlayWindow = null;
        MemoryOptimizer.TrimWorkingSet();
    }

    private void OnDisplayConfigurationChanged(object? sender, EventArgs e)
    {
        if (!IsFrozen || CurrentFrozenMonitor == null)
        {
            StartLiveMirroringIfAvailable();
            return;
        }

        var availableMonitors = _monitorService.GetAllMonitors();
        bool stillConnected = availableMonitors.Any(m => m.DeviceName == CurrentFrozenMonitor.DeviceName);

        if (!stillConnected)
        {
            Resume();
        }
    }

    private void OnStateChanged(bool isFrozen, MonitorInfo? target)
    {
        StateChanged?.Invoke(this, new FreezeStateChangedEventArgs
        {
            IsFrozen = isFrozen,
            TargetMonitor = target
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _relocationService.Dispose();
        _monitorService.DisplayConfigurationChanged -= OnDisplayConfigurationChanged;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(CloseOverlay);
        }
        else
        {
            CloseOverlay();
        }

        // Auto-restore display topology to Duplicate (Clone) on exit (FR-00)
        _topologyService.RestoreOnExit();
    }
}
