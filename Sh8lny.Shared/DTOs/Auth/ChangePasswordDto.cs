using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.Auth;

/// <summary>
/// DTO for changing the authenticated user's password.
/// </summary>
public class ChangePasswordDto
{
    /// <summary>The user's current password for verification.</summary>
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>The new password to set.</summary>
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Confirmation of the new password — must match NewPassword.</summary>
    [Required]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
