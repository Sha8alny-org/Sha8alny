namespace Sh8lny.Domain.Models;

/// <summary>
/// Singleton row storing application-wide configuration (maintenance mode, version gate).
/// Only one row should ever exist in the database (Id = 1).
/// </summary>
public class AppConfig
{
    /// <summary>Primary key (always 1 for the singleton row).</summary>
    public int Id { get; set; }

    /// <summary>When true, the mobile app should block entry and show the maintenance screen.</summary>
    public bool IsMaintenanceMode { get; set; }

    /// <summary>Title displayed on the maintenance screen.</summary>
    public string MaintenanceTitle { get; set; } = string.Empty;

    /// <summary>Body text displayed on the maintenance screen.</summary>
    public string MaintenanceMessage { get; set; } = string.Empty;

    /// <summary>Semver string (e.g. "1.0.0") — mobile must be at least this version.</summary>
    public string MinSupportedVersion { get; set; } = "1.0.0";

    /// <summary>Last time this row was updated.</summary>
    public DateTime UpdatedAt { get; set; }
}
