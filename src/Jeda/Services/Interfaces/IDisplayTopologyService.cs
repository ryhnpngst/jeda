namespace Jeda.Services.Interfaces;

public enum DisplayTopology
{
    Unknown = 0,
    Internal = 1,
    Clone = 2,
    Extend = 4,
    External = 8
}

/// <summary>
/// Service interface for detecting and managing Windows display topology (FR-00).
/// </summary>
public interface IDisplayTopologyService
{
    DisplayTopology GetCurrentTopology();
    bool WasInitiallyClone { get; }
    void InitializeAtStartup();
    bool SwitchToExtend();
    bool SwitchToClone();
    void RestoreOnExit();
}

