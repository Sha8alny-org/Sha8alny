using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Shared.DTOs.Common;
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
    /// Gets a training submission by its ID.
    /// </summary>
    /// <param name="id">The submission ID.</param>
    /// <returns>The training submission details.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse<TrainingSubmissionResponseDto>>> GetById(int id)
    {
        var result = await _trainingSubmissionService.GetByIdAsync(id);
        
        if (!result.IsSuccess)
        {
            return NotFound(result);
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
