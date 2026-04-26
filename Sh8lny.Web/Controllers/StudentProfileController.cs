using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Projects;
using Sh8lny.Shared.DTOs.StudentProfile;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for student profile management.
/// </summary>
[ApiController]
[Route("api/students")]
[Authorize]
public class StudentProfileController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly IProjectService _projectService;

    public StudentProfileController(IStudentService studentService, IProjectService projectService)
    {
        _studentService = studentService;
        _projectService = projectService;
    }

    /// <summary>
    /// Creates a complete student profile with education, experience, and skills.
    /// </summary>
    /// <param name="dto">The profile data.</param>
    /// <returns>The created student ID.</returns>
    [HttpPost("profile")]
    public async Task<ActionResult<ServiceResponse<int>>> CreateProfile([FromBody] CreateStudentProfileDto dto)
    {
        // Extract UserId from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ServiceResponse<int>.Failure("Invalid or missing user token."));
        }

        var result = await _studentService.CreateProfileAsync(userId, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates the student profile for the authenticated user.
    /// </summary>
    /// <param name="dto">The updated profile data.</param>
    /// <returns>The updated student ID.</returns>
    [HttpPut("profile")]
    public async Task<ActionResult<ServiceResponse<int>>> UpdateProfile([FromBody] StudentProfileUpdateDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ServiceResponse<int>.Failure("Invalid or missing user token."));
        }

        var result = await _studentService.UpdateStudentProfileAsync(userId, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Searches for students based on the provided criteria.
    /// </summary>
    /// <param name="searchDto">The search criteria.</param>
    /// <returns>A paged list of student search results.</returns>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceResponse<PagedResult<StudentSearchResultDto>>>> Search([FromQuery] StudentSearchDto searchDto)
    {
        var result = await _studentService.SearchStudentsAsync(searchDto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the student profile for the authenticated user.
    /// </summary>
    /// <returns>The student profile.</returns>
    [HttpGet("profile")]
    public async Task<ActionResult<ServiceResponse<StudentResponseDto>>> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ServiceResponse<StudentResponseDto>.Failure("Invalid or missing user token."));
        }

        var result = await _studentService.GetProfileAsync(userId);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("saved-projects/{projectId}")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<ServiceResponse<bool>>> ToggleSavedProject(int projectId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ServiceResponse<bool>.Failure("Invalid or missing user token."));
        }

        var result = await _projectService.ToggleSaveProjectAsync(userId, projectId);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("saved-projects")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<SavedProjectResponseDto>>>> GetSavedProjects()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ServiceResponse<IEnumerable<SavedProjectResponseDto>>.Failure("Invalid or missing user token."));
        }

        var result = await _projectService.GetSavedProjectsAsync(userId);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
