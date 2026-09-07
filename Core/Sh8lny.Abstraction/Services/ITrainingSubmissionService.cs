using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Training;
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

    /// <summary>
    /// Gets a filtered, paginated list of student training/internship records.
    /// Admin-only (Training Unit).
    /// </summary>
    /// <param name="userId">The requesting user ID (from JWT claims).</param>
    /// <param name="filter">The filter criteria and pagination parameters.</param>
    /// <returns>Service response with the paged training records.</returns>
    Task<ServiceResponse<PagedResult<TrainingRecordListItemDto>>> GetFilteredTrainingRecordsAsync(int userId, TrainingRecordFilterDto filter);

    /// <summary>
    /// Submits the 5 training deliverables (Certificate, Report, Presentation,
    /// CompanyEvaluation, StudentSurvey) for an application, creating per-file
    /// records that the Training Unit can review individually.
    /// </summary>
    /// <param name="studentUserId">The student's user ID (from JWT claims).</param>
    /// <param name="dto">The deliverable document URLs.</param>
    /// <returns>Service response with the created submission detail.</returns>
    Task<ServiceResponse<TrainingSubmissionDetailDto>> SubmitDeliverablesAsync(int studentUserId, SubmitTrainingDeliverablesDto dto);

    /// <summary>
    /// Training Unit admin reviews the deliverables of a submission, approving or
    /// rejecting each file with an optional rejection reason. If all files are
    /// approved the submission is academically approved; if any file is rejected
    /// the submission is marked Rejected and the student may re-upload that file.
    /// </summary>
    /// <param name="submissionId">The submission ID to review.</param>
    /// <param name="adminUserId">The admin's user ID (from JWT claims).</param>
    /// <param name="dto">The per-file review decisions.</param>
    /// <returns>Service response with the updated submission detail.</returns>
    Task<ServiceResponse<TrainingSubmissionDetailDto>> ReviewDeliverablesAsync(int submissionId, int adminUserId, ReviewTrainingDeliverablesDto dto);

    /// <summary>
    /// Student re-uploads a single rejected deliverable. Updates only that file's
    /// URL, resets its status to Pending, clears its rejection reason, and returns
    /// the overall submission to Pending review. Approved files are untouched.
    /// </summary>
    /// <param name="studentUserId">The student's user ID (from JWT claims).</param>
    /// <param name="submissionId">The submission ID.</param>
    /// <param name="dto">The deliverable type and new URL.</param>
    /// <returns>Service response with the updated submission detail.</returns>
    Task<ServiceResponse<TrainingSubmissionDetailDto>> ReuploadDeliverableAsync(int studentUserId, int submissionId, ReuploadSingleDeliverableDto dto);

    /// <summary>
    /// Gets a training submission with the per-file deliverable breakdown.
    /// </summary>
    /// <param name="submissionId">The submission ID.</param>
    /// <returns>Service response with the submission detail.</returns>
    Task<ServiceResponse<TrainingSubmissionDetailDto>> GetSubmissionDetailAsync(int submissionId);

    /// <summary>
    /// Training Unit admin overrides the final approved training duration (in days).
    /// Used when the actual certificate proves a duration different from the
    /// project's original listing. The override takes priority when crediting days
    /// at completion.
    /// </summary>
    /// <param name="adminUserId">The admin's user ID (from JWT claims).</param>
    /// <param name="submissionId">The submission ID.</param>
    /// <param name="dto">The approved duration (days) and optional justification.</param>
    /// <returns>Service response with the updated submission detail.</returns>
    Task<ServiceResponse<TrainingSubmissionDetailDto>> OverrideDurationAsync(int adminUserId, int submissionId, OverrideTrainingDurationDto dto);
}
