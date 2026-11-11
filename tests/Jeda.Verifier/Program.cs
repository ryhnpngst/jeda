using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Jeda.Models;
using Jeda.Services;
using Jeda.Services.Interfaces;
using Jeda.ViewModels;

namespace Jeda.Verifier;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        Console.WriteLine("=== JEDA Comprehensive Verification (M1 - M4 + Auto Topology & Live Mirroring) ===");

        try
        {
            if (Application.Current == null)
            {
                _ = new Application();
            }

            var topologyService = new DisplayTopologyService();
            var currentTopology = topologyService.GetCurrentTopology();
            Console.WriteLine($"\n[TOPOLOGY] Topologi Display Sistem Saat Ini: {currentTopology}");

            using var monitorService = new MonitorService();
            var screenCaptureService = new ScreenCaptureService();
            using var freezeService = new FreezeService(monitorService, screenCaptureService, topologyService);

            // 1. Monitors
            var monitors = monitorService.GetAllMonitors();
            Console.WriteLine($"[TEST 1] Monitor Terdeteksi: {monitors.Count}");
            foreach (var m in monitors)
            {
                Console.WriteLine($"   - {m.DisplayTitle}");
            }

            var defaultTarget = monitorService.GetDefaultTargetMonitor()
                                ?? throw new Exception("FAIL: Target monitor null!");

            // 2. Freeze Test
            Console.WriteLine($"\n[TEST 2] Membekukan layar eksternal ({defaultTarget.DisplayTitle})...");
            freezeService.Freeze(defaultTarget);
            if (!freezeService.IsFrozen)
            {
                throw new Exception("FAIL: Status harus Frozen setelah Freeze()!");
            }
            Console.WriteLine("   -> SUKSES: Layar eksternal berhasil dibekukan (Mirroring dijeda).");

            // 3. Resume Test
            Console.WriteLine("[TEST 3] Melanjutkan layar eksternal (Resume)...");
            freezeService.Resume();
            if (freezeService.IsFrozen)
            {
                throw new Exception("FAIL: Status harus Idle/Live setelah Resume()!");
            }
            Console.WriteLine("   -> SUKSES: Layar eksternal kembali live mirroring.");

            // 4. MainViewModel Test
            var vm = new MainViewModel(monitorService, freezeService);
            Console.WriteLine($"\n[TEST 4] Status ViewModel: {vm.Status.Message}");
            vm.ToggleFreezeCommand.Execute(null);
            Console.WriteLine($"   -> Setelah Toggle Hotkey: {vm.Status.Message}, Tombol: '{vm.ActionButtonText}'");
            if (!vm.IsFrozen) throw new Exception("FAIL: ViewModel harus berstatus Frozen!");

            vm.ToggleFreezeCommand.Execute(null);
            Console.WriteLine($"   -> Setelah Resume: {vm.Status.Message}, Tombol: '{vm.ActionButtonText}'");
            if (vm.IsFrozen) throw new Exception("FAIL: ViewModel harus kembali Ready!");

            Console.WriteLine("\n[SUCCESS] SEMUA VERIFIKASI ALUR KERJA BARU BERHASIL!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR] {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            return 1;
        }
    }
}
