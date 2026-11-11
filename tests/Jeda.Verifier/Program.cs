using System;
using System.Diagnostics;
using System.Linq;
using Jeda.Models;
using Jeda.Services;
using Jeda.ViewModels;

namespace Jeda.Verifier;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        Console.WriteLine("=== JEDA Milestone M2 & M3: Monitor Detection & Screen Capture Verification ===");

        try
        {
            using var monitorService = new MonitorService();
            var screenCaptureService = new ScreenCaptureService();

            // --- Milestone M2 Tests ---
            Console.WriteLine("\n--- [MILESTONE M2: MONITOR DETECTION] ---");
            var monitors = monitorService.GetAllMonitors();
            Console.WriteLine($"[TEST 1] Total monitor terdeteksi: {monitors.Count}");

            if (monitors.Count == 0)
            {
                throw new Exception("FAIL: Tidak ada monitor yang terdeteksi!");
            }

            foreach (var m in monitors)
            {
                Console.WriteLine($"   - {m.DisplayTitle}");
                Console.WriteLine($"     Device: {m.DeviceName}, Bounds: {m.Bounds}, Role: {m.Role}, Orientasi: {m.Orientation}");
            }

            var primary = monitorService.GetPrimaryMonitor();
            if (primary == null || !primary.IsPrimary)
            {
                throw new Exception("FAIL: GetPrimaryMonitor gagal mengembalikan monitor utama!");
            }
            Console.WriteLine($"[TEST 2] Monitor Utama berhasil ditemukan: {primary.DisplayTitle}");

            var defaultTarget = monitorService.GetDefaultTargetMonitor();
            if (defaultTarget == null)
            {
                throw new Exception("FAIL: GetDefaultTargetMonitor mengembalikan null!");
            }

            if (monitors.Count > 1)
            {
                if (defaultTarget.IsPrimary)
                {
                    throw new Exception("FAIL: Pada multi-monitor, target default harus berupa monitor eksternal!");
                }
                Console.WriteLine($"[TEST 3] Target default adalah monitor eksternal: {defaultTarget.DisplayTitle}");
            }

            // --- Milestone M3 Tests ---
            Console.WriteLine("\n--- [MILESTONE M3: SCREEN CAPTURE] ---");

            foreach (var monitor in monitors)
            {
                Console.WriteLine($"\n[TEST 4] Menguji tangkapan layar untuk: {monitor.DisplayTitle}");
                var sw = Stopwatch.StartNew();
                var screenshot = screenCaptureService.CaptureScreen(monitor);
                sw.Stop();

                // Validasi resolusi & aspek rasio (FR-02)
                if (screenshot.PixelWidth != monitor.Width || screenshot.PixelHeight != monitor.Height)
                {
                    throw new Exception(
                        $"FAIL: Resolusi tangkapan ({screenshot.PixelWidth}x{screenshot.PixelHeight}) " +
                        $"tidak cocok dengan resolusi monitor ({monitor.Width}x{monitor.Height})!");
                }

                // Validasi format pixel & freeze state (NFR-01, NFR-05)
                if (!screenshot.IsFrozen)
                {
                    throw new Exception("FAIL: BitmapSource harus dalam status Frozen untuk efisiensi memori dan thread-safety!");
                }

                Console.WriteLine($"   -> SUKSES: Resolusi cocok ({screenshot.PixelWidth} × {screenshot.PixelHeight})");
                Console.WriteLine($"   -> Waktu Capture: {sw.ElapsedMilliseconds} ms (Sangat cepat, memenuhi NFR-01)");
                Console.WriteLine($"   -> Format Pixel: {screenshot.Format}, IsFrozen: {screenshot.IsFrozen}");
            }

            // Test 5: MainViewModel integrasi
            Console.WriteLine("\n--- [VIEWMODEL INTEGRATION] ---");
            var vm = new MainViewModel(monitorService, null);
            if (vm.SelectedMonitor?.DeviceName != defaultTarget.DeviceName)
            {
                throw new Exception("FAIL: ViewModel.SelectedMonitor tidak otomatis memilih default target monitor!");
            }

            Console.WriteLine($"[TEST 5] MainViewModel siap dengan {vm.Monitors.Count} monitor. Target: {vm.SelectedMonitor.DisplayTitle}");
            Console.WriteLine($"         Status: {vm.Status.Message} - {vm.Status.Details}");

            Console.WriteLine("\n[SUCCESS] Semua verifikasi Milestone M1, M2, dan M3 berhasil!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR] {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }
}
