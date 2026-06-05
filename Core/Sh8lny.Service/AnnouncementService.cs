using Microsoft.Extensions.Logging;
using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Announcements;
using Sh8lny.Shared.DTOs.Common;

namespace Sh8lny.Service;

/// <summary>
/// Manages platform-wide announcements (CRUD).
/// </summary>
public class AnnouncementService : IAnnouncementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AnnouncementService> _logger;

    public AnnouncementService(IUnitOfWork unitOfWork, ILogger<AnnouncementService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<List<AnnouncementDto>>> GetAnnouncementsAsync()
    {
        try
        {
            var announcements = await _unitOfWork.Announcements.GetAllAsync();

            var dtos = announcements
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AnnouncementDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    ImageUrl = a.ImageUrl,
                    Link = a.Link,
                    CreatedAt = a.CreatedAt
                })
                .ToList();

            return ServiceResponse<List<AnnouncementDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving announcements.");
            return ServiceResponse<List<AnnouncementDto>>.Failure("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<AnnouncementDto>> CreateAsync(CreateAnnouncementDto dto)
    {
        try
        {
            var announcement = new Announcement
            {
                Title = dto.Title,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                Link = dto.Link,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Announcements.AddAsync(announcement);
            await _unitOfWork.SaveAsync();

            var result = new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Description = announcement.Description,
                ImageUrl = announcement.ImageUrl,
                Link = announcement.Link,
                CreatedAt = announcement.CreatedAt
            };

            _logger.LogInformation("Announcement {AnnouncementId} created.", announcement.Id);
            return ServiceResponse<AnnouncementDto>.Success(result, "Announcement created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating announcement.");
            return ServiceResponse<AnnouncementDto>.Failure("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<AnnouncementDto>> UpdateAsync(int id, CreateAnnouncementDto dto)
    {
        try
        {
            var announcement = await _unitOfWork.Announcements.GetByIdAsync(id);

            if (announcement is null)
                return ServiceResponse<AnnouncementDto>.Failure("Announcement not found.");

            announcement.Title = dto.Title;
            announcement.Description = dto.Description;
            announcement.ImageUrl = dto.ImageUrl;
            announcement.Link = dto.Link;
            announcement.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Announcements.Update(announcement);
            await _unitOfWork.SaveAsync();

            var result = new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Description = announcement.Description,
                ImageUrl = announcement.ImageUrl,
                Link = announcement.Link,
                CreatedAt = announcement.CreatedAt
            };

            _logger.LogInformation("Announcement {AnnouncementId} updated.", id);
            return ServiceResponse<AnnouncementDto>.Success(result, "Announcement updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating announcement {AnnouncementId}.", id);
            return ServiceResponse<AnnouncementDto>.Failure("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> DeleteAsync(int id)
    {
        try
        {
            var announcement = await _unitOfWork.Announcements.GetByIdAsync(id);

            if (announcement is null)
                return ServiceResponse<bool>.Failure("Announcement not found.");

            _unitOfWork.Announcements.Remove(announcement);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Announcement {AnnouncementId} deleted.", id);
            return ServiceResponse<bool>.Success(true, "Announcement deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting announcement {AnnouncementId}.", id);
            return ServiceResponse<bool>.Failure("An unexpected error occurred.");
        }
    }
}
