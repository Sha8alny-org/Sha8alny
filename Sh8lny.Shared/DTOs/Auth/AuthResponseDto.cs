namespace Sh8lny.Shared.DTOs.Auth;

/// <summary>
/// DTO for authentication response containing JWT token and user info.
/// </summary>
public class AuthResponseDto
{
    public bool IsSuccess { get; set; }
    public string? Token { get; set; }
    public DateTime? Expiration { get; set; }
    public int? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Message { get; set; }
}
