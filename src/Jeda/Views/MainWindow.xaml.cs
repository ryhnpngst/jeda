using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Jeda.Native;
using Jeda.Services;
using Jeda.Services.Interfaces;
using Jeda.ViewModels;

namespace Jeda.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml (FR-01, FR-05, FR-06).
/// </summary>
public partial class MainWindow : Window
{
    private IHotkeyService? _hotkeyService;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Register Global Hotkey (Ctrl + Shift + Z) (FR-05)
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            _hotkeyService = new HotkeyService(hwnd);
            _hotkeyService.RegisterHotKey(
                HotKeyModifiers.Control | HotKeyModifiers.Shift,
                Key.Z,
                () =>
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.ToggleFreezeCommand.Execute(null);
                    }
                });
        }
        catch
        {
            // Graceful fallback if hotkey registration is not permitted
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _hotkeyService?.Dispose();

        // Ensure clean disposal of services, live mirror overlays, and restore display topology (FR-00, NFR-02)
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
