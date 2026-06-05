using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.Auth;

/// <summary>
/// DTO for updating the user's FCM push notification device token.
/// </summary>
public class UpdateFcmTokenDto
{
    /// <summary>
    /// The FCM registration token for the user's device.
    /// </summary>
    [Required]
    public string FcmToken { get; set; } = string.Empty;
}
