namespace Sh8lny.Domain.Models;

/// <summary>
/// Platform-wide announcement displayed on the mobile home screen.
/// </summary>
public class Announcement
{
    /// <summary>Primary key (auto-increment).</summary>
    public int Id { get; set; }

    /// <summary>Announcement title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Announcement body text.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Optional image URL (uploaded via /api/Media).</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Optional external or deep link URL.</summary>
    public string? Link { get; set; }

    /// <summary>When the announcement was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the announcement was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}
