using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Jeda.Models;
using Jeda.Services;
using Jeda.Services.Interfaces;

namespace Jeda.ViewModels;

/// <summary>
/// Main ViewModel driving the primary user interface and coordinating monitor state and actions (FR-00, FR-01, FR-06, FR-03, FR-04).
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IMonitorService _monitorService;
    private readonly IFreezeService? _freezeService;

    private MonitorInfo? _selectedMonitor;
    private AppStatus _status = AppStatus.Ready();
    private bool _isFrozen;
    private bool _disposed;

    public MainViewModel() : this(CreateDefaultServices())
    {
    }

    private static (IMonitorService Monitor, IFreezeService Freeze) CreateDefaultServices()
    {
        var monitorService = new MonitorService();
        var captureService = new ScreenCaptureService();
        var topologyService = new DisplayTopologyService();
        var freezeService = new FreezeService(monitorService, captureService, topologyService);
        return (monitorService, freezeService);
    }

    private MainViewModel((IMonitorService Monitor, IFreezeService Freeze) services)
        : this(services.Monitor, services.Freeze)
    {
    }

    public MainViewModel(IMonitorService monitorService, IFreezeService? freezeService)
    {
        _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
        _freezeService = freezeService;

        Monitors = new ObservableCollection<MonitorInfo>();

        ToggleFreezeCommand = new RelayCommand(ExecuteToggleFreeze, CanExecuteToggleFreeze);
        RefreshMonitorsCommand = new RelayCommand(RefreshMonitors);

        if (_freezeService != null)
        {
            _freezeService.StateChanged += OnFreezeStateChanged;
        }

        _monitorService.DisplayConfigurationChanged += OnDisplayConfigurationChanged;

        RefreshMonitors();
    }

    public ObservableCollection<MonitorInfo> Monitors { get; }

    public MonitorInfo? SelectedMonitor
    {
        get => _selectedMonitor;
        set
        {
            if (SetProperty(ref _selectedMonitor, value))
            {
                OnPropertyChanged(nameof(HasSelectedMonitor));
                OnPropertyChanged(nameof(SelectedMonitorDetails));
                (ToggleFreezeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasSelectedMonitor => SelectedMonitor != null;

    public string SelectedMonitorDetails => SelectedMonitor != null
        ? SelectedMonitor.DisplayTitle
        : "Tidak ada monitor yang dipilih";

    public string MonitorSummary => Monitors.Count == 1
        ? "1 Monitor terdeteksi"
        : $"{Monitors.Count} Monitor terdeteksi";

    public AppStatus Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool IsFrozen
    {
        get => _isFrozen;
        private set
        {
            if (SetProperty(ref _isFrozen, value))
            {
                OnPropertyChanged(nameof(ActionButtonText));
                (ToggleFreezeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string ActionButtonText => IsFrozen ? "Lanjutkan Layar (Resume)" : "Bekukan Layar (Freeze)";

    public string HotkeyHint => "Ctrl + Shift + Z";

    public ICommand ToggleFreezeCommand { get; }
    public ICommand RefreshMonitorsCommand { get; }

    public void RefreshMonitors()
    {
        var previousDeviceName = _selectedMonitor?.DeviceName;
        Monitors.Clear();

        var detected = _monitorService.GetAllMonitors();
        foreach (var monitor in detected)
        {
            Monitors.Add(monitor);
        }

        SelectedMonitor = Monitors.FirstOrDefault(m => m.DeviceName == previousDeviceName)
                          ?? _monitorService.GetDefaultTargetMonitor()
                          ?? Monitors.FirstOrDefault();

        OnPropertyChanged(nameof(MonitorSummary));

        if (!IsFrozen)
        {
            Status = Monitors.Count > 0
                ? AppStatus.Ready()
                : AppStatus.Error("Tidak ada monitor yang terdeteksi.");
        }
    }

    private void OnDisplayConfigurationChanged(object? sender, EventArgs e)
    {
        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(RefreshMonitors);
        }
        else
        {
            RefreshMonitors();
        }
    }

    private bool CanExecuteToggleFreeze() => SelectedMonitor != null;

    private void ExecuteToggleFreeze()
    {
        if (SelectedMonitor == null) return;

        try
        {
            if (_freezeService != null)
            {
                _freezeService.Toggle(SelectedMonitor);
            }
        }
        catch (Exception ex)
        {
            Status = AppStatus.Error("Gagal membekukan layar", ex.Message);
        }
    }

    private void OnFreezeStateChanged(object? sender, FreezeStateChangedEventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => UpdateStateFromEvent(e));
        }
        else
        {
            UpdateStateFromEvent(e);
        }
    }

    private void UpdateStateFromEvent(FreezeStateChangedEventArgs e)
    {
        IsFrozen = e.IsFrozen;
        Status = e.IsFrozen && e.TargetMonitor != null
            ? AppStatus.Frozen(e.TargetMonitor.DisplayTitle)
            : AppStatus.Ready();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _monitorService.DisplayConfigurationChanged -= OnDisplayConfigurationChanged;
        _monitorService.Dispose();

        if (_freezeService != null)
        {
            _freezeService.StateChanged -= OnFreezeStateChanged;
            _freezeService.Dispose();
        }
    }
}
