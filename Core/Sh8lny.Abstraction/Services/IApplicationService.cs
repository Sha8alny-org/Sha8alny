using Sh8lny.Shared.DTOs.Applications;
using Sh8lny.Shared.DTOs.Common;

namespace Sh8lny.Abstraction.Services;

/// <summary>
/// Interface for application operations.
/// </summary>
public interface IApplicationService
{
    /// <summary>
    /// Submits an application for a project.
    /// </summary>
    /// <param name="studentUserId">The user ID of the student.</param>
    /// <param name="dto">The application data.</param>
    /// <returns>Service response containing the application ID.</returns>
    Task<ServiceResponse<int>> ApplyForProjectAsync(int studentUserId, CreateApplicationDto dto);

    /// <summary>
    /// Gets all applications submitted by a student.
    /// </summary>
    /// <param name="studentUserId">The user ID of the student.</param>
    /// <returns>Service response containing the list of applications.</returns>
    Task<ServiceResponse<IEnumerable<ApplicationResponseDto>>> GetStudentApplicationsAsync(int studentUserId);

    /// <summary>
    /// Gets all applications for a specific project (company view).
    /// </summary>
    /// <param name="companyUserId">The user ID of the company.</param>
    /// <param name="projectId">The project ID.</param>
    /// <returns>Service response containing the list of applicants.</returns>
    Task<ServiceResponse<IEnumerable<ApplicantDto>>> GetProjectApplicationsAsync(int companyUserId, int projectId);

    /// <summary>
    /// Updates the status of an application (accept/reject).
    /// </summary>
    /// <param name="companyUserId">The user ID of the company.</param>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="dto">The status update data.</param>
    /// <returns>Service response indicating success or failure.</returns>
    Task<ServiceResponse<bool>> UpdateApplicationStatusAsync(int companyUserId, int applicationId, UpdateApplicationStatusDto dto);

    /// <summary>
    /// Reviews an application (accept/reject) and sends notification to student.
    /// </summary>
    /// <param name="companyUserId">The user ID of the company.</param>
    /// <param name="dto">The review data including status and optional note.</param>
    /// <returns>Service response indicating success or failure.</returns>
    Task<ServiceResponse<bool>> ReviewApplicationAsync(int companyUserId, ReviewApplicationDto dto);

    /// <summary>
    /// Gets the applicants for a project ranked by GPA (highest first).
    /// Only the company owning the project or an Admin may access this ranking.
    /// </summary>
    /// <param name="userId">The user ID of the requester (Company owner or Admin).</param>
    /// <param name="projectId">The project ID.</param>
    /// <param name="count">Optional limit for the top N applicants.</param>
    /// <returns>Service response containing the ranked applicants.</returns>
    Task<ServiceResponse<IEnumerable<RankedApplicantDto>>> GetApplicantsRankedByGpaAsync(int userId, int projectId, int? count = null);

    /// <summary>
    /// Withdraws an application (student action).
    /// </summary>
    /// </summary>
    /// <param name="studentUserId">The user ID of the student.</param>
    /// <param name="applicationId">The application ID.</param>
    /// <returns>Service response indicating success or failure.</returns>
    Task<ServiceResponse<bool>> WithdrawApplicationAsync(int studentUserId, int applicationId);
}
