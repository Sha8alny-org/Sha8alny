using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Shared.DTOs.Applications;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Projects;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for project/opportunity management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IApplicationService _applicationService;

    public ProjectsController(IProjectService projectService, IApplicationService applicationService)
    {
        _projectService = projectService;
        _applicationService = applicationService;
    }

    /// <summary>
    /// Creates a new project/opportunity.
    /// </summary>
    /// <param name="dto">The project data.</param>
    /// <returns>The created project ID.</returns>
    [HttpPost]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<int>>> CreateProject([FromBody] CreateProjectDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<int>.Failure("Invalid or missing user token."));
        }

        var result = await _projectService.CreateProjectAsync(userId.Value, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetProjectById), new { id = result.Data }, result);
    }

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="dto">The updated project data.</param>
    /// <returns>Success or failure response.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateProject(int id, [FromBody] UpdateProjectDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<bool>.Failure("Invalid or missing user token."));
        }

        var result = await _projectService.UpdateProjectAsync(userId.Value, id, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Deletes a project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <returns>Success or failure response.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteProject(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<bool>.Failure("Invalid or missing user token."));
        }

        var result = await _projectService.DeleteProjectAsync(userId.Value, id);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets a project by ID.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <returns>The project details.</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceResponse<ProjectResponseDto>>> GetProjectById(int id)
    {
        var result = await _projectService.GetProjectByIdAsync(id);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all projects for the current company.
    /// </summary>
    /// <returns>List of company projects.</returns>
    [HttpGet("my-projects")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<ProjectResponseDto>>>> GetMyProjects()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<IEnumerable<ProjectResponseDto>>.Failure("Invalid or missing user token."));
        }

        var result = await _projectService.GetCompanyProjectsAsync(userId.Value);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Advanced search: filters, sorts, and paginates projects.
    /// </summary>
    /// <param name="filter">
    /// Query params: Keyword, ProjectType, Status, CompanyId,
    /// DeadlineBefore, DeadlineAfter, SkillIds, SortBy, PageNumber, PageSize.
    /// </param>
    /// <returns>Paginated list of matching projects.</returns>
    /// <remarks>
    /// Example: GET /api/projects/search?Keyword=flutter&amp;SkillIds=1&amp;SkillIds=3&amp;Status=Active&amp;SortBy=newest&amp;PageSize=20
    /// </remarks>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceResponse<PagedResult<ProjectResponseDto>>>> SearchProjects(
        [FromQuery] ProjectFilterDto filter)
    {
        var result = await _projectService.GetFilteredProjectsAsync(filter);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the applicants for a project ranked by GPA (highest first).
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="count">Optional limit for the top N applicants.</param>
    /// <returns>Ranked list of applicants.</returns>
    /// <remarks>
    /// Example: GET /api/projects/5/applicants/ranked-by-gpa?count=10
    /// </remarks>
    [HttpGet("{projectId}/applicants/ranked-by-gpa")]
    [Authorize(Roles = "Company,Admin")]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<RankedApplicantDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<RankedApplicantDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<RankedApplicantDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ServiceResponse<IEnumerable<RankedApplicantDto>>>> GetApplicantsRankedByGpa(
        int projectId, [FromQuery] int? count)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<IEnumerable<RankedApplicantDto>>.Failure("Invalid or missing user token."));
        }

        var result = await _applicationService.GetApplicantsRankedByGpaAsync(userId.Value, projectId, count);

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
