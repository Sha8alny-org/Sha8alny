using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.TrainingSubmission;

namespace Sh8lny.Abstraction.Services;

/// <summary>
/// Service for managing field training/internship document submissions.
/// Implements a dual-approval workflow requiring both Admin academic approval and Company industry verification.
/// </summary>
public interface ITrainingSubmissionService
{
    /// <summary>
    /// Submits training documents by a student.
    /// Creates a new TrainingSubmission with Status = Pending.
    /// </summary>
    /// <param name="studentId">The student ID (from JWT claims).</param>
    /// <param name="dto">The training documents to submit.</param>
    /// <returns>Service response with the created submission.</returns>
    Task<ServiceResponse<TrainingSubmissionResponseDto>> SubmitDocumentsAsync(int studentId, SubmitTrainingDocumentsDto dto);

    /// <summary>
    /// Admin reviews a training submission (approve/reject).
    /// Sets IsAdminApproved and checks if submission can be fully completed.
    /// </summary>
    /// <param name="submissionId">The submission ID to review.</param>
    /// <param name="adminId">The admin user ID (from JWT claims).</param>
    /// <param name="dto">The admin review decision.</param>
    /// <returns>Service response with the updated submission.</returns>
    Task<ServiceResponse<TrainingSubmissionResponseDto>> AdminReviewAsync(int submissionId, int adminId, AdminReviewTrainingDto dto);

    /// <summary>
    /// Company verifies a training submission.
    /// Sets IsCompanyVerified and checks if submission can be fully completed.
    /// </summary>
    /// <param name="submissionId">The submission ID to verify.</param>
    /// <param name="companyId">The company ID (from JWT claims).</param>
    /// <returns>Service response with the updated submission.</returns>
    Task<ServiceResponse<TrainingSubmissionResponseDto>> CompanyVerifyAsync(int submissionId, int companyId);

    /// <summary>
    /// Gets a training submission by its ID.
    /// </summary>
    /// <param name="submissionId">The submission ID.</param>
    /// <returns>Service response with the submission details.</returns>
    Task<ServiceResponse<TrainingSubmissionResponseDto>> GetByIdAsync(int submissionId);

    /// <summary>
    /// Gets all training submissions for a specific application.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <returns>Service response with list of submissions.</returns>
    Task<ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>> GetByApplicationAsync(int applicationId);

    /// <summary>
    /// Gets all training submissions for a student.
    /// </summary>
    /// <param name="studentId">The student ID.</param>
    /// <returns>Service response with list of submissions.</returns>
    Task<ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>> GetByStudentAsync(int studentId);

    /// <summary>
    /// Gets all pending submissions awaiting admin review.
    /// </summary>
    /// <returns>Service response with list of pending submissions.</returns>
    Task<ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>> GetPendingForAdminAsync();

    /// <summary>
    /// Gets all submissions pending company verification for a specific company.
    /// </summary>
    /// <param name="companyId">The company ID.</param>
    /// <returns>Service response with list of submissions pending company verification.</returns>
    Task<ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>> GetPendingForCompanyAsync(int companyId);
}
