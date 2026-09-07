using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Shared.DTOs.Applications;
using Sh8lny.Shared.DTOs.Common;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for application management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    /// <summary>
    /// Submits an application for a project.
    /// Training/internship opportunities with a GPA requirement reject applicants
    /// whose GPA is below the project's minimum.
    /// </summary>
    /// <param name="dto">The application data.</param>
    /// <returns>The created application ID.</returns>
    [HttpPost("apply")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ServiceResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<int>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<int>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ServiceResponse<int>>> Apply([FromBody] CreateApplicationDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<int>.Failure("Invalid or missing user token."));
        }

        var result = await _applicationService.ApplyForProjectAsync(userId.Value, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all applications submitted by the current student.
    /// </summary>
    /// <returns>List of student applications.</returns>
    [HttpGet("my-applications")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<ApplicationResponseDto>>>> GetMyApplications()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<IEnumerable<ApplicationResponseDto>>.Failure("Invalid or missing user token."));
        }

        var result = await _applicationService.GetStudentApplicationsAsync(userId.Value);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all applications for a specific project (company view).
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <returns>List of applicants.</returns>
    [HttpGet("project/{projectId}")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<ApplicantDto>>>> GetProjectApplications(int projectId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<IEnumerable<ApplicantDto>>.Failure("Invalid or missing user token."));
        }

        var result = await _applicationService.GetProjectApplicationsAsync(userId.Value, projectId);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates the status of an application (accept/reject).
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="dto">The status update data.</param>
    /// <returns>Success or failure response.</returns>
    [HttpPut("{applicationId}/status")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateStatus(int applicationId, [FromBody] UpdateApplicationStatusDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<bool>.Failure("Invalid or missing user token."));
        }

        var result = await _applicationService.UpdateApplicationStatusAsync(userId.Value, applicationId, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Reviews an application (accept/reject) and notifies the student.
    /// </summary>
    /// <param name="dto">The review data.</param>
    /// <returns>Success or failure response.</returns>
    [HttpPut("review")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<bool>>> Review([FromBody] ReviewApplicationDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<bool>.Failure("Invalid or missing user token."));
        }

        var result = await _applicationService.ReviewApplicationAsync(userId.Value, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Withdraws an application.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <returns>Success or failure response.</returns>
    [HttpPost("{applicationId}/withdraw")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<ServiceResponse<bool>>> Withdraw(int applicationId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<bool>.Failure("Invalid or missing user token."));
        }

        var result = await _applicationService.WithdrawApplicationAsync(userId.Value, applicationId);

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
