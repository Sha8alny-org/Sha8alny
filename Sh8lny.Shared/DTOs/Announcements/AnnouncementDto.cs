namespace Sh8lny.Shared.DTOs.Announcements;

/// <summary>
/// DTO returned by GET /api/Announcements — view model for announcement list.
/// </summary>
public class AnnouncementDto
{
    /// <summary>Announcement ID.</summary>
    public int Id { get; set; }

    /// <summary>Announcement title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Announcement body text.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Optional image URL.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Optional link URL.</summary>
    public string? Link { get; set; }

    /// <summary>When the announcement was created.</summary>
    public DateTime CreatedAt { get; set; }
}
