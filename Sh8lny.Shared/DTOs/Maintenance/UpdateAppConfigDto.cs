using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.Maintenance;

/// <summary>
/// DTO sent to PUT /api/Maintenance/config — admin-only update of the app configuration.
/// </summary>
public class UpdateAppConfigDto
{
    /// <summary>Enable or disable maintenance mode.</summary>
    [Required]
    public bool IsMaintenanceMode { get; set; }

    /// <summary>Title shown on the maintenance screen.</summary>
    [Required]
    [MaxLength(200)]
    public string MaintenanceTitle { get; set; } = string.Empty;

    /// <summary>Body message shown on the maintenance screen.</summary>
    [Required]
    [MaxLength(1000)]
    public string MaintenanceMessage { get; set; } = string.Empty;

    /// <summary>Minimum app version required (semver, e.g. "1.0.0").</summary>
    [Required]
    [MaxLength(20)]
    public string MinSupportedVersion { get; set; } = "1.0.0";
}
