using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Maintenance;

namespace Sh8lny.Abstraction.Services;

/// <summary>
/// Service for managing application-wide configuration (maintenance mode, version gate).
/// Separate from <see cref="IBackupService"/> which handles database backups.
/// </summary>
public interface IMaintenanceService
{
    /// <summary>
    /// Returns the current app configuration. Creates a default row if none exists.
    /// </summary>
    Task<ServiceResponse<AppConfigDto>> GetAppConfigAsync();

    /// <summary>
    /// Updates the app configuration singleton row.
    /// </summary>
    Task<ServiceResponse<bool>> UpdateAppConfigAsync(UpdateAppConfigDto dto);
}
