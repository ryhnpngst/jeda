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
        Console.WriteLine("=== JEDA Comprehensive Verification Suite (Milestone M7 Testing & Validation) ===");

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

            // 5. TrayService Test (M6)
            using (var tray = new TrayService(
                () => { },
                () => { },
                () => { }))
            {
                tray.UpdateState(true, "Monitor 2");
                tray.UpdateState(false);
            }
            Console.WriteLine("[TEST 5] TrayService (M6): Berhasil dibuat, diperbarui (Freeze/Resume), dan dibersihkan.");

            // 6. ScreenCaptureService Direct Capture Test (M3 & FR-02)
            var capturedBitmap = screenCaptureService.CaptureScreen(defaultTarget);
            if (capturedBitmap == null || !capturedBitmap.IsFrozen)
            {
                throw new Exception("FAIL: Hasil tangkapan layar harus valid dan ter-freeze!");
            }
            Console.WriteLine($"[TEST 6] ScreenCaptureService (M3): Berhasil menangkap layar {capturedBitmap.PixelWidth}x{capturedBitmap.PixelHeight} (Format: {capturedBitmap.Format}).");

            // 7. Repeated Freeze & Resume Stress Test (NFR-02 Reliability)
            Console.WriteLine("[TEST 7] Stress Test (NFR-02): Menjalankan 10 siklus Freeze/Resume berturut-turut...");
            for (int cycle = 1; cycle <= 10; cycle++)
            {
                freezeService.Freeze(defaultTarget);
                if (!freezeService.IsFrozen) throw new Exception($"FAIL pada siklus {cycle}: Freeze gagal!");
                freezeService.Resume();
                if (freezeService.IsFrozen) throw new Exception($"FAIL pada siklus {cycle}: Resume gagal!");
            }
            Console.WriteLine("   -> SUKSES: 10 siklus Freeze/Resume selesai tanpa crash atau kebocoran status.");

            // 8. Memory Management Test (NFR-01)
            Jeda.Utilities.MemoryOptimizer.TrimWorkingSet();
            Console.WriteLine("[TEST 8] MemoryOptimizer (NFR-01): Berhasil memicu Garbage Collection dan flush working set.");

            Console.WriteLine("\n========================================================");
            Console.WriteLine("=== SEMUA PENGUJIAN MILESTONE M7 BERHASIL DILALUI!   ===");
            Console.WriteLine("========================================================");
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
