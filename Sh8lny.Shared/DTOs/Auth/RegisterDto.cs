namespace Sh8lny.Shared.DTOs.Auth;

/// <summary>
/// DTO for user registration.
/// </summary>
public class RegisterDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Role { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
