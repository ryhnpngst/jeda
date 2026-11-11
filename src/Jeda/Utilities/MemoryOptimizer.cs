using System.Diagnostics;
using Jeda.Native;

namespace Jeda.Utilities;

/// <summary>
/// Utility to optimize and trim working set memory for JEDA.
/// Keeps physical memory footprint low and returns unused committed heap pages to the OS.
/// </summary>
public static class MemoryOptimizer
{
    /// <summary>
    /// Forces a non-blocking generation-2 garbage collection and flushes the process working set.
    /// </summary>
    public static void TrimWorkingSet()
    {
        try
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: false, compacting: true);
            GC.WaitForPendingFinalizers();

            using var process = Process.GetCurrentProcess();
            if (!process.HasExited)
            {
                NativeMethods.SetProcessWorkingSetSize(process.Handle, -1, -1);
            }
        }
        catch
        {
            // Best effort; ignore if OS restricts working set modification
        }
    }
}

