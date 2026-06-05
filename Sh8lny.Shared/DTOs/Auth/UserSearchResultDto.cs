namespace Sh8lny.Shared.DTOs.Auth;

/// <summary>
/// DTO returned by the unified user search endpoint.
/// Contains enough information to display a user in a chat contact list.
/// </summary>
public class UserSearchResultDto
{
    /// <summary>The user's ID (UserID).</summary>
    public int UserId { get; set; }

    /// <summary>Full display name (student name or company name).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>User type: Student, Company, Admin, University.</summary>
    public string UserType { get; set; } = string.Empty;

    /// <summary>URL to the user's profile picture or company logo.</summary>
    public string? ProfilePictureUrl { get; set; }
}
