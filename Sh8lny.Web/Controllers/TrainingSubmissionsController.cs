using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Training;
using Sh8lny.Shared.DTOs.TrainingSubmission;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for managing field training/internship document submissions.
/// Implements a dual-approval workflow requiring both Admin academic approval and Company industry verification.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrainingSubmissionsController : ControllerBase
{
    private readonly ITrainingSubmissionService _trainingSubmissionService;

    public TrainingSubmissionsController(ITrainingSubmissionService trainingSubmissionService)
    {
        _trainingSubmissionService = trainingSubmissionService;
    }

    /// <summary>
    /// Submits training documents by a student.
    /// All document URLs should be obtained from /api/Media upload endpoints.
    /// </summary>
    /// <param name="dto">The training documents to submit.</param>
    /// <returns>The created training submission.</returns>
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<ServiceResponse<TrainingSubmissionResponseDto>>> SubmitDocuments([FromBody] SubmitTrainingDocumentsDto dto)
    {
        var studentId = GetCurrentUserId();
        if (studentId == null)
        {
            return Unauthorized(ServiceResponse<TrainingSubmissionResponseDto>.Failure("User not authenticated."));
        }

        var result = await _trainingSubmissionService.SubmitDocumentsAsync(studentId.Value, dto);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.TrainingSubmissionID }, result);
    }

    /// <summary>
    /// Gets a training submission by its ID, including the per-file deliverable breakdown.
    /// </summary>
    /// <param name="id">The submission ID.</param>
    /// <returns>The training submission details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceResponse<TrainingSubmissionDetailDto>>> GetById(int id)
    {
        var result = await _trainingSubmissionService.GetSubmissionDetailAsync(id);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Submits the 5 training deliverables (Certificate, Report, Presentation,
    /// CompanyEvaluation, StudentSurvey) for an application.
    /// All document URLs should be obtained from /api/Media upload endpoints.
    /// </summary>
    /// <param name="dto">The deliverable document URLs.</param>
    /// <returns>The created submission with the per-file breakdown.</returns>
    [HttpPost("deliverables")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ServiceResponse<TrainingSubmissionDetailDto>>> SubmitDeliverables(
        [FromBody] SubmitTrainingDeliverablesDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ServiceResponse<TrainingSubmissionDetailDto>.Failure("User not authenticated."));
        }

        var result = await _trainingSubmissionService.SubmitDeliverablesAsync(userId.Value, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.TrainingSubmissionID }, result);
    }

    /// <summary>
    /// Training Unit admin reviews the deliverables of a submission, approving or
    /// rejecting each file. A rejection reason is required for rejected files.
    /// </summary>
    /// <param name="id">The submission ID to review.</param>
    /// <param name="dto">The per-file review decisions.</param>
    /// <returns>The updated submission with the per-file breakdown.</returns>
    [HttpPost("{id}/review")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ServiceResponse<TrainingSubmissionDetailDto>>> ReviewDeliverables(
        int id, [FromBody] ReviewTrainingDeliverablesDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ServiceResponse<TrainingSubmissionDetailDto>.Failure("User not authenticated."));
        }

        var result = await _trainingSubmissionService.ReviewDeliverablesAsync(id, userId.Value, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Re-uploads a single rejected deliverable. Only the rejected file is updated;
    /// approved documents are preserved.
    /// </summary>
    /// <param name="id">The submission ID.</param>
    /// <param name="dto">The deliverable type and the new URL (from /api/Media).</param>
    /// <returns>The updated submission with the per-file breakdown.</returns>
    [HttpPut("{id}/reupload")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ServiceResponse<TrainingSubmissionDetailDto>>> ReuploadDeliverable(
        int id, [FromBody] ReuploadSingleDeliverableDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ServiceResponse<TrainingSubmissionDetailDto>.Failure("User not authenticated."));
        }

        var result = await _trainingSubmissionService.ReuploadDeliverableAsync(userId.Value, id, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all training submissions for the current student.
    /// </summary>
    /// <returns>List of training submissions.</returns>
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>>> GetMySubmissions()
    {
        var studentId = GetCurrentUserId();
        if (studentId == null)
        {
            return Unauthorized(ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>.Failure("User not authenticated."));
        }

        var result = await _trainingSubmissionService.GetByStudentAsync(studentId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Admin reviews a training submission (approve/reject).
    /// </summary>
    /// <param name="id">The submission ID to review.</param>
    /// <param name="dto">The admin review decision.</param>
    /// <returns>The updated training submission.</returns>
    [HttpPut("{id}/admin-review")]
    [Authorize(Roles = "Admin,University")]
    public async Task<ActionResult<ServiceResponse<TrainingSubmissionResponseDto>>> AdminReview(int id, [FromBody] AdminReviewTrainingDto dto)
    {
        var adminId = GetCurrentUserId();
        if (adminId == null)
        {
            return Unauthorized(ServiceResponse<TrainingSubmissionResponseDto>.Failure("User not authenticated."));
        }

        var result = await _trainingSubmissionService.AdminReviewAsync(id, adminId.Value, dto);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Company verifies a training submission.
    /// </summary>
    /// <param name="id">The submission ID to verify.</param>
    /// <returns>The updated training submission.</returns>
    [HttpPut("{id}/company-verify")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<TrainingSubmissionResponseDto>>> CompanyVerify(int id)
    {
        var companyId = GetCurrentUserId();
        if (companyId == null)
        {
            return Unauthorized(ServiceResponse<TrainingSubmissionResponseDto>.Failure("User not authenticated."));
        }

        var result = await _trainingSubmissionService.CompanyVerifyAsync(id, companyId.Value);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all pending submissions awaiting admin review.
    /// </summary>
    /// <returns>List of pending submissions.</returns>
    [HttpGet("pending-admin")]
    [Authorize(Roles = "Admin,University")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>>> GetPendingForAdmin()
    {
        var result = await _trainingSubmissionService.GetPendingForAdminAsync();
        return Ok(result);
    }

    /// <summary>
    /// Gets all submissions pending company verification for the current company.
    /// </summary>
    /// <returns>List of submissions pending company verification.</returns>
    [HttpGet("pending-company")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>>> GetPendingForCompany()
    {
        var companyId = GetCurrentUserId();
        if (companyId == null)
        {
            return Unauthorized(ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>.Failure("User not authenticated."));
        }

        var result = await _trainingSubmissionService.GetPendingForCompanyAsync(companyId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Advanced search over student training/internship records (Training Unit, Admin only).
    /// </summary>
    /// <param name="filter">
    /// Query params: CompanyId, StartDate, EndDate, DepartmentId (null = all departments),
    /// AcademicYear, ProjectType ("Training"/"Internship"), PageNumber, PageSize.
    /// </param>
    /// <returns>Paginated list of matching training records.</returns>
    /// <remarks>
    /// Example: GET /api/TrainingSubmissions/records/filter?StartDate=2020-01-01&amp;EndDate=2024-12-31&amp;DepartmentId=3&amp;PageNumber=1&amp;PageSize=10
    /// </remarks>
    [HttpGet("records/filter")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ServiceResponse<PagedResult<TrainingRecordListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<PagedResult<TrainingRecordListItemDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<PagedResult<TrainingRecordListItemDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ServiceResponse<PagedResult<TrainingRecordListItemDto>>>> GetFilteredTrainingRecords(
        [FromQuery] TrainingRecordFilterDto filter)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ServiceResponse<PagedResult<TrainingRecordListItemDto>>.Failure("User not authenticated."));
        }

        var result = await _trainingSubmissionService.GetFilteredTrainingRecordsAsync(userId.Value, filter);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Training Unit admin overrides the final approved training duration (in days).
    /// Used when the actual certificate proves a duration different from the
    /// project's original listing. The override takes priority when crediting days.
    /// </summary>
    /// <param name="id">The submission ID.</param>
    /// <param name="dto">The approved duration (days) and optional justification.</param>
    /// <returns>The updated submission with the per-file breakdown.</returns>
    [HttpPut("{id}/override-duration")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<TrainingSubmissionDetailDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ServiceResponse<TrainingSubmissionDetailDto>>> OverrideDuration(
        int id, [FromBody] OverrideTrainingDurationDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ServiceResponse<TrainingSubmissionDetailDto>.Failure("User not authenticated."));
        }

        var result = await _trainingSubmissionService.OverrideDurationAsync(userId.Value, id, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the current user's ID from JWT claims.
    /// </summary>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
