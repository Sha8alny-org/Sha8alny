using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Shared.DTOs.Admin;
using Sh8lny.Shared.DTOs.Common;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for admin management operations.
/// Requires Admin role for all endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Gets dashboard statistics for the admin panel.
    /// </summary>
    /// <returns>Dashboard statistics.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<ServiceResponse<AdminDashboardStatsDto>>> GetDashboardStats()
    {
        var result = await _adminService.GetDashboardStatsAsync();

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all users for management.
    /// </summary>
    /// <returns>List of all users.</returns>
    [HttpGet("users")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<UserManagementDto>>>> GetAllUsers()
    {
        var result = await _adminService.GetAllUsersAsync();

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets a specific user by ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>User details.</returns>
    [HttpGet("users/{id}")]
    public async Task<ActionResult<ServiceResponse<UserManagementDto>>> GetUserById(int id)
    {
        var result = await _adminService.GetUserByIdAsync(id);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Toggles a user's ban status (ban/unban).
    /// </summary>
    /// <param name="id">The user ID to ban/unban.</param>
    /// <returns>Success response with new status.</returns>
    [HttpPut("users/{id}/ban")]
    public async Task<ActionResult<ServiceResponse<bool>>> ToggleUserBan(int id)
    {
        var result = await _adminService.ToggleUserBanAsync(id);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all projects for management.
    /// </summary>
    /// <returns>List of all projects.</returns>
    [HttpGet("projects")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<ProjectManagementDto>>>> GetAllProjects()
    {
        var result = await _adminService.GetAllProjectsAsync();

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Force deletes a project (admin bypass).
    /// </summary>
    /// <param name="id">The project ID to delete.</param>
    /// <returns>Success or failure response.</returns>
    [HttpDelete("projects/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteProjectForce(int id)
    {
        var result = await _adminService.DeleteProjectForceAsync(id);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets historical dashboard metric snapshots for trend analysis.
    /// </summary>
    /// <param name="days">Number of days of history to retrieve (default 30).</param>
    /// <returns>List of metric snapshots ordered by date.</returns>
    [HttpGet("metrics/history")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<DashboardMetricHistoryDto>>>> GetMetricHistory([FromQuery] int days = 30)
    {
        var result = await _adminService.GetMetricHistoryAsync(days);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Manually records a daily snapshot of current dashboard metrics.
    /// Idempotent — skips if today's snapshot already exists.
    /// </summary>
    /// <returns>Success or failure response.</returns>
    [HttpPost("metrics/snapshot")]
    public async Task<ActionResult<ServiceResponse<bool>>> RecordDailySnapshot()
    {
        var result = await _adminService.RecordDailySnapshotAsync();

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
