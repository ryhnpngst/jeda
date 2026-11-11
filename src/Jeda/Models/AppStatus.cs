namespace Jeda.Models;

/// <summary>
/// Status codes matching PRD FR-08 (Ready, Frozen, Error).
/// </summary>
public enum AppStatusCode
{
    Ready,
    Frozen,
    Error
}

/// <summary>
/// Immutable snapshot of current application status.
/// </summary>
public sealed record AppStatus(
    AppStatusCode Code,
    string Message,
    string? Details = null
)
{
    public static AppStatus Ready() =>
        new(AppStatusCode.Ready, "Siap (Ready)", "Aplikasi aktif. Tekan Ctrl+Shift+Z untuk membekukan layar.");

    public static AppStatus Frozen(string monitorName) =>
        new(AppStatusCode.Frozen, "Dibekukan (Frozen)", $"Layar {monitorName} sedang dibekukan.");

    public static AppStatus Error(string message, string? details = null) =>
        new(AppStatusCode.Error, "Kesalahan (Error)", details ?? message);
}

