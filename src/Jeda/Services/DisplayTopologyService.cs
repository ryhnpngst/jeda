using System.Diagnostics;
using Jeda.Native;
using Jeda.Services.Interfaces;

namespace Jeda.Services;

/// <summary>
/// Manages automatic switching between Windows Duplicate (Clone) and Extend display modes (FR-00).
/// </summary>
public sealed class DisplayTopologyService : IDisplayTopologyService
{
    private bool _wasInitiallyClone;
    private bool _initialized;

    public bool WasInitiallyClone => _wasInitiallyClone;

    public DisplayTopology GetCurrentTopology()
    {
        try
        {
            int err = NativeMethods.GetDisplayConfigBufferSizes(
                NativeMethods.QDC_DATABASE_CURRENT,
                out uint pathCount,
                out uint modeCount);

            if (err != 0 || pathCount == 0 || modeCount == 0)
            {
                return DisplayTopology.Unknown;
            }

            byte[] paths = new byte[pathCount * 72];
            byte[] modes = new byte[modeCount * 64];

            var queryResult = NativeMethods.QueryDisplayConfig(
                NativeMethods.QDC_DATABASE_CURRENT,
                ref pathCount,
                paths,
                ref modeCount,
                modes,
                out uint topology);

            if (queryResult == QueryDisplayConfigResult.Success)
            {
                return topology switch
                {
                    NativeMethods.SDC_TOPOLOGY_INTERNAL => DisplayTopology.Internal,
                    NativeMethods.SDC_TOPOLOGY_CLONE => DisplayTopology.Clone,
                    NativeMethods.SDC_TOPOLOGY_EXTEND => DisplayTopology.Extend,
                    NativeMethods.SDC_TOPOLOGY_EXTERNAL => DisplayTopology.External,
                    _ => DisplayTopology.Unknown
                };
            }
        }
        catch
        {
            // Fallback gracefully
        }

        return DisplayTopology.Unknown;
    }

    /// <summary>
    /// Checks initial topology at startup. If in Clone mode, automatically switches to Extend (FR-00).
    /// </summary>
    public void InitializeAtStartup()
    {
        if (_initialized) return;
        _initialized = true;

        var current = GetCurrentTopology();
        _wasInitiallyClone = (current == DisplayTopology.Clone);

        if (_wasInitiallyClone)
        {
            SwitchToExtend();
        }
    }

    public bool SwitchToExtend()
    {
        try
        {
            int result = NativeMethods.SetDisplayConfig(
                0,
                IntPtr.Zero,
                0,
                IntPtr.Zero,
                NativeMethods.SDC_APPLY | NativeMethods.SDC_TOPOLOGY_EXTEND);

            if (result == 0) return true;
        }
        catch
        {
            // Ignore and fallback to displayswitch.exe
        }

        return RunDisplaySwitch("/extend");
    }

    public bool SwitchToClone()
    {
        try
        {
            int result = NativeMethods.SetDisplayConfig(
                0,
                IntPtr.Zero,
                0,
                IntPtr.Zero,
                NativeMethods.SDC_APPLY | NativeMethods.SDC_TOPOLOGY_CLONE);

            if (result == 0) return true;
        }
        catch
        {
            // Ignore and fallback to displayswitch.exe
        }

        return RunDisplaySwitch("/clone");
    }

    public void RestoreOnExit()
    {
        // Revert to Duplicate if it was Clone initially, or enforce duplicate on exit as required by user flow
        if (_wasInitiallyClone || GetCurrentTopology() == DisplayTopology.Extend)
        {
            SwitchToClone();
        }
    }

    private static bool RunDisplaySwitch(string argument)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "displayswitch.exe",
                Arguments = argument,
                UseShellExecute = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

