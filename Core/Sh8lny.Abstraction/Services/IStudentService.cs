using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.StudentProfile;

namespace Sh8lny.Abstraction.Services;

/// <summary>
/// Interface for student profile operations.
/// </summary>
public interface IStudentService
{
    /// <summary>
    /// Creates a complete student profile with education, experience, and skills.
    /// </summary>
    /// <param name="userId">The ID of the user creating the profile.</param>
    /// <param name="dto">The profile data.</param>
    /// <returns>Service response containing the created student ID.</returns>
    Task<ServiceResponse<int>> CreateProfileAsync(int userId, CreateStudentProfileDto dto);

    /// <summary>
    /// Updates the student profile for the authenticated user.
    /// </summary>
    /// <param name="userId">The ID of the user updating the profile.</param>
    /// <param name="dto">The updated profile data.</param>
    /// <returns>Service response containing the updated student ID.</returns>
    Task<ServiceResponse<int>> UpdateStudentProfileAsync(int userId, StudentProfileUpdateDto dto);

    /// <summary>
    /// Searches for students based on the provided criteria.
    /// </summary>
    /// <param name="searchDto">The search criteria.</param>
    /// <returns>Service response containing a paged list of student search results.</returns>
    Task<ServiceResponse<PagedResult<StudentSearchResultDto>>> SearchStudentsAsync(StudentSearchDto searchDto);

    /// <summary>
    /// Gets the student profile for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>Service response containing the student profile.</returns>
    Task<ServiceResponse<StudentResponseDto>> GetProfileAsync(int userId);
}
