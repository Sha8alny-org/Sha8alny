using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.Announcements;

/// <summary>
/// DTO sent to POST /api/Announcements and PUT /api/Announcements/{id} — admin create/update payload.
/// </summary>
public class CreateAnnouncementDto
{
    /// <summary>Announcement title.</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Announcement body text.</summary>
    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Optional image URL (pre-uploaded via /api/Media).</summary>
    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    /// <summary>Optional external or deep link URL.</summary>
    [MaxLength(1000)]
    public string? Link { get; set; }
}
