using Sh8lny.Shared.DTOs.Announcements;
using Sh8lny.Shared.DTOs.Common;

namespace Sh8lny.Abstraction.Services;

/// <summary>
/// Service for managing platform-wide announcements.
/// </summary>
public interface IAnnouncementService
{
    /// <summary>
    /// Returns all announcements ordered by CreatedAt descending.
    /// </summary>
    Task<ServiceResponse<List<AnnouncementDto>>> GetAnnouncementsAsync();

    /// <summary>
    /// Creates a new announcement. Admin only.
    /// </summary>
    Task<ServiceResponse<AnnouncementDto>> CreateAsync(CreateAnnouncementDto dto);

    /// <summary>
    /// Updates an existing announcement by ID. Admin only.
    /// </summary>
    Task<ServiceResponse<AnnouncementDto>> UpdateAsync(int id, CreateAnnouncementDto dto);

    /// <summary>
    /// Deletes an announcement by ID. Admin only.
    /// </summary>
    Task<ServiceResponse<bool>> DeleteAsync(int id);
}
