namespace Sh8lny.Shared.DTOs.Maintenance;

/// <summary>
/// DTO returned by GET /api/Maintenance/config — read-only view of the app configuration.
/// </summary>
public class AppConfigDto
{
    /// <summary>Whether the app is currently in maintenance mode.</summary>
    public bool IsMaintenanceMode { get; set; }

    /// <summary>Title shown on the maintenance screen.</summary>
    public string MaintenanceTitle { get; set; } = string.Empty;

    /// <summary>Body message shown on the maintenance screen.</summary>
    public string MaintenanceMessage { get; set; } = string.Empty;

    /// <summary>Minimum app version required (semver).</summary>
    public string MinSupportedVersion { get; set; } = "1.0.0";
}
