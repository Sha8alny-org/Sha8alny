using Sh8lny.Shared.DTOs.Admin;
using Sh8lny.Shared.DTOs.Common;

namespace Sh8lny.Abstraction.Services;

/// <summary>
/// Interface for admin management operations.
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Gets dashboard statistics for the admin panel.
    /// </summary>
    /// <returns>Service response containing dashboard stats.</returns>
    Task<ServiceResponse<AdminDashboardStatsDto>> GetDashboardStatsAsync();

    /// <summary>
    /// Gets all users for management purposes.
    /// </summary>
    /// <returns>Service response containing list of users.</returns>
    Task<ServiceResponse<IEnumerable<UserManagementDto>>> GetAllUsersAsync();

    /// <summary>
    /// Gets all projects for management purposes.
    /// </summary>
    /// <returns>Service response containing list of projects.</returns>
    Task<ServiceResponse<IEnumerable<ProjectManagementDto>>> GetAllProjectsAsync();

    /// <summary>
    /// Toggles a user's ban status (ban/unban).
    /// </summary>
    /// <param name="userId">The user ID to ban/unban.</param>
    /// <returns>Service response indicating success and new status.</returns>
    Task<ServiceResponse<bool>> ToggleUserBanAsync(int userId);

    /// <summary>
    /// Force deletes a project (admin bypass - ignores ownership).
    /// </summary>
    /// <param name="projectId">The project ID to delete.</param>
    /// <returns>Service response indicating success or failure.</returns>
    Task<ServiceResponse<bool>> DeleteProjectForceAsync(int projectId);

    /// <summary>
    /// Gets a specific user's details for management.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Service response containing user details.</returns>
    Task<ServiceResponse<UserManagementDto>> GetUserByIdAsync(int userId);

    /// <summary>
    /// Gets historical dashboard metric snapshots for trend analysis.
    /// </summary>
    /// <param name="days">Number of days of history to retrieve (default 30).</param>
    /// <returns>Service response containing list of metric snapshots ordered by date.</returns>
    Task<ServiceResponse<IEnumerable<DashboardMetricHistoryDto>>> GetMetricHistoryAsync(int days = 30);

    /// <summary>
    /// Records a daily snapshot of current dashboard metrics.
    /// Skips recording if a snapshot for today already exists.
    /// </summary>
    /// <returns>Service response indicating success or failure.</returns>
    Task<ServiceResponse<bool>> RecordDailySnapshotAsync();
}
