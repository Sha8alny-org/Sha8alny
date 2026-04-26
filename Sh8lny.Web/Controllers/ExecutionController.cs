using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Execution;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for project execution (module management and progress tracking).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExecutionController : ControllerBase
{
    private readonly IProjectExecutionService _executionService;

    public ExecutionController(IProjectExecutionService executionService)
    {
        _executionService = executionService;
    }

    /// <summary>
    /// Adds a new module (milestone) to a project.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="dto">The module data.</param>
    /// <returns>The created module ID.</returns>
    [HttpPost("project/{projectId}/modules")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<int>>> AddModule(int projectId, [FromBody] CreateProjectModuleDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<int>.Failure("Invalid or missing user token."));
        }

        var result = await _executionService.AddModuleAsync(userId.Value, projectId, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all modules for a project.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <returns>List of project modules.</returns>
    [HttpGet("project/{projectId}/modules")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<IEnumerable<ProjectModuleDto>>>> GetProjectModules(int projectId)
    {
        var result = await _executionService.GetProjectModulesAsync(projectId);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Deletes a module from a project.
    /// </summary>
    /// <param name="moduleId">The module ID.</param>
    /// <returns>Success or failure response.</returns>
    [HttpDelete("modules/{moduleId}")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteModule(int moduleId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<bool>.Failure("Invalid or missing user token."));
        }

        var result = await _executionService.DeleteModuleAsync(userId.Value, moduleId);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates progress on a module for an application.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="dto">The progress update data.</param>
    /// <returns>Success or failure response.</returns>
    [HttpPut("application/{applicationId}/progress")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateProgress(int applicationId, [FromBody] UpdateProgressDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<bool>.Failure("Invalid or missing user token."));
        }

        var result = await _executionService.UpdateProgressAsync(userId.Value, applicationId, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the progress summary for an application.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <returns>Progress summary with all modules.</returns>
    [HttpGet("application/{applicationId}/progress")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<ProjectProgressDto>>> GetApplicationProgress(int applicationId)
    {
        var result = await _executionService.GetApplicationProgressAsync(applicationId);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Reviews a module milestone (approve/reject) by the owning company.
    /// </summary>
    /// <param name="moduleId">The module ID.</param>
    /// <param name="dto">Review status and optional company feedback.</param>
    /// <returns>Success or failure response.</returns>
    [HttpPut("modules/{moduleId}/review")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<bool>>> ReviewModule(int moduleId, [FromBody] ReviewModuleDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<bool>.Failure("Invalid or missing user token."));
        }

        var result = await _executionService.ReviewModuleAsync(userId.Value, moduleId, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Completes a job/project formally after all modules are finished.
    /// Only the company owner can complete the job.
    /// </summary>
    /// <param name="dto">The completion data including feedback and deliverable URL.</param>
    /// <returns>Completion summary with statistics.</returns>
    [HttpPost("complete")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<CompletionSummaryDto>>> CompleteJob([FromBody] CompleteJobDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<CompletionSummaryDto>.Failure("Invalid or missing user token."));
        }

        var result = await _executionService.CompleteJobAsync(userId.Value, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Extracts the current user ID from JWT claims.
    /// </summary>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}
