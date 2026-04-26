using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Execution;

namespace Sh8lny.Abstraction.Services;

/// <summary>
/// Interface for project execution operations (modules and progress tracking).
/// </summary>
public interface IProjectExecutionService
{
    /// <summary>
    /// Adds a new module (milestone) to a project.
    /// </summary>
    /// <param name="companyUserId">The company user ID.</param>
    /// <param name="projectId">The project ID.</param>
    /// <param name="dto">The module data.</param>
    /// <returns>Service response containing the created module ID.</returns>
    Task<ServiceResponse<int>> AddModuleAsync(int companyUserId, int projectId, CreateProjectModuleDto dto);

    /// <summary>
    /// Gets all modules for a project.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <returns>Service response containing the list of modules.</returns>
    Task<ServiceResponse<IEnumerable<ProjectModuleDto>>> GetProjectModulesAsync(int projectId);

    /// <summary>
    /// Updates progress on a module for an application.
    /// </summary>
    /// <param name="studentUserId">The student user ID.</param>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="dto">The progress update data.</param>
    /// <returns>Service response indicating success or failure.</returns>
    Task<ServiceResponse<bool>> UpdateProgressAsync(int studentUserId, int applicationId, UpdateProgressDto dto);

    /// <summary>
    /// Gets the progress summary for an application.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <returns>Service response containing the progress summary.</returns>
    Task<ServiceResponse<ProjectProgressDto>> GetApplicationProgressAsync(int applicationId);

    /// <summary>
    /// Reviews a module progress update by company (approve/reject).
    /// </summary>
    /// <param name="companyUserId">The company user ID (must own project).</param>
    /// <param name="moduleId">The module ID.</param>
    /// <param name="dto">Review decision and optional feedback.</param>
    /// <returns>Service response indicating success or failure.</returns>
    Task<ServiceResponse<bool>> ReviewModuleAsync(int companyUserId, int moduleId, ReviewModuleDto dto);

    /// <summary>
    /// Deletes a module from a project.
    /// </summary>
    /// <param name="companyUserId">The company user ID.</param>
    /// <param name="moduleId">The module ID.</param>
    /// <returns>Service response indicating success or failure.</returns>
    Task<ServiceResponse<bool>> DeleteModuleAsync(int companyUserId, int moduleId);

    /// <summary>
    /// Completes a job/project formally by the company after all modules are finished.
    /// </summary>
    /// <param name="companyUserId">The company user ID (must be the project owner).</param>
    /// <param name="dto">The completion data including feedback and deliverable URL.</param>
    /// <returns>Service response containing the completion summary.</returns>
    Task<ServiceResponse<CompletionSummaryDto>> CompleteJobAsync(int companyUserId, CompleteJobDto dto);
}
