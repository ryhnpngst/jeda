using System.Collections.ObjectModel;
using System.Windows.Input;
using Jeda.Models;
using Jeda.Services.Interfaces;

namespace Jeda.ViewModels;

/// <summary>
/// Main ViewModel driving the primary user interface and coordinating monitor state and actions.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly IMonitorService? _monitorService;
    private readonly IFreezeService? _freezeService;

    private MonitorInfo? _selectedMonitor;
    private AppStatus _status = AppStatus.Ready();
    private bool _isFrozen;

    public MainViewModel() : this(null, null)
    {
    }

    public MainViewModel(IMonitorService? monitorService, IFreezeService? freezeService)
    {
        _monitorService = monitorService;
        _freezeService = freezeService;

        Monitors = new ObservableCollection<MonitorInfo>();

        ToggleFreezeCommand = new RelayCommand(ExecuteToggleFreeze, CanExecuteToggleFreeze);
        RefreshMonitorsCommand = new RelayCommand(RefreshMonitors);

        if (_freezeService != null)
        {
            _freezeService.StateChanged += OnFreezeStateChanged;
        }

        if (_monitorService != null)
        {
            _monitorService.DisplayConfigurationChanged += (_, _) => RefreshMonitors();
        }

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
                (ToggleFreezeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

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
        Monitors.Clear();

        if (_monitorService != null)
        {
            foreach (var monitor in _monitorService.GetAllMonitors())
            {
                Monitors.Add(monitor);
            }
            SelectedMonitor = _monitorService.GetDefaultTargetMonitor() ?? Monitors.FirstOrDefault();
        }
        else
        {
            // Placeholder fallback for M1 milestone when services are not yet plugged in
            var fallback = new MonitorInfo
            {
                Index = 1,
                DeviceName = @"\\.\DISPLAY1",
                FriendlyName = "Monitor 1 (Internal Display)",
                Bounds = new System.Drawing.Rectangle(0, 0, 1920, 1080),
                WorkingArea = new System.Drawing.Rectangle(0, 0, 1920, 1040),
                IsPrimary = true
            };
            Monitors.Add(fallback);
            SelectedMonitor = fallback;
        }

        if (!IsFrozen)
        {
            Status = AppStatus.Ready();
        }
    }

    private bool CanExecuteToggleFreeze() => SelectedMonitor != null;

    private void ExecuteToggleFreeze()
    {
        if (SelectedMonitor == null) return;

        if (_freezeService != null)
        {
            _freezeService.Toggle(SelectedMonitor);
        }
        else
        {
            // Simulation for M1 milestone verification
            IsFrozen = !IsFrozen;
            Status = IsFrozen
                ? AppStatus.Frozen(SelectedMonitor.DisplayTitle)
                : AppStatus.Ready();
        }
    }

    private void OnFreezeStateChanged(object? sender, FreezeStateChangedEventArgs e)
    {
        IsFrozen = e.IsFrozen;
        Status = e.IsFrozen && e.TargetMonitor != null
            ? AppStatus.Frozen(e.TargetMonitor.DisplayTitle)
            : AppStatus.Ready();
    }
}

