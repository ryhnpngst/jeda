using System;
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
        Console.WriteLine("=== JEDA Milestone M2: Monitor Detection Verification ===");

        try
        {
            using var monitorService = new MonitorService();

            // Test 1: GetAllMonitors
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

            // Test 2: Primary Monitor
            var primary = monitorService.GetPrimaryMonitor();
            if (primary == null || !primary.IsPrimary)
            {
                throw new Exception("FAIL: GetPrimaryMonitor gagal mengembalikan monitor utama!");
            }
            Console.WriteLine($"[TEST 2] Monitor Utama berhasil ditemukan: {primary.DisplayTitle}");

            // Test 3: Default Target Monitor
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
                Console.WriteLine($"[TEST 3] Multi-monitor terdeteksi. Target default adalah monitor eksternal: {defaultTarget.DisplayTitle}");
            }
            else
            {
                Console.WriteLine($"[TEST 3] Single-monitor terdeteksi. Target default adalah: {defaultTarget.DisplayTitle}");
            }

            // Test 4: MainViewModel binding with MonitorService
            var vm = new MainViewModel(monitorService, null);
            if (vm.Monitors.Count != monitors.Count)
            {
                throw new Exception($"FAIL: ViewModel.Monitors.Count ({vm.Monitors.Count}) != Service monitors count ({monitors.Count})");
            }

            if (vm.SelectedMonitor?.DeviceName != defaultTarget.DeviceName)
            {
                throw new Exception("FAIL: ViewModel.SelectedMonitor tidak otomatis memilih default target monitor!");
            }

            Console.WriteLine($"[TEST 4] MainViewModel berhasil memuat {vm.Monitors.Count} monitor dan memilih: {vm.SelectedMonitor.DisplayTitle}");
            Console.WriteLine($"         Summary: {vm.MonitorSummary}");
            Console.WriteLine($"         Details: {vm.SelectedMonitorDetails}");

            Console.WriteLine("\n[SUCCESS] Semua verifikasi Milestone M2 berhasil!");
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

