using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Shared.DTOs.Auth;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for authentication endpoints (register and login).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="dto">Registration data containing email, password, and role.</param>
    /// <returns>Authentication response with JWT token if successful.</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="dto">Login credentials containing email and password.</param>
    /// <returns>Authentication response with JWT token if successful.</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (!result.IsSuccess)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the current authenticated user's basic information.
    /// </summary>
    /// <returns>User summary including display name and profile picture.</returns>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserSummaryDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Invalid or missing user token." });
        }

        var result = await _authService.GetCurrentUserAsync(userId);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Sends a 6-digit password reset code to the user's email.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var result = await _authService.ForgotPasswordAsync(dto.Email);
        if (!result.IsSuccess)
        {
            return BadRequest(new { Message = result.Message });
        }
        return Ok(new { Message = result.Data });
    }

    /// <summary>
    /// Resets the user's password using the 6-digit code.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var result = await _authService.ResetPasswordAsync(dto);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Message });

        return Ok(new { Message = result.Data });
    }

    /// <summary>
    /// Updates the authenticated user's FCM push notification device token.
    /// </summary>
    /// <param name="dto">The new FCM token.</param>
    /// <returns>Success or failure response.</returns>
    [Authorize]
    [HttpPut("fcm-token")]
    public async Task<IActionResult> UpdateFcmToken([FromBody] UpdateFcmTokenDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Invalid or missing user token." });
        }

        var result = await _authService.UpdateFcmTokenAsync(userId, dto.FcmToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Changes the authenticated user's password.
    /// </summary>
    /// <param name="dto">Current password, new password, and confirmation.</param>
    /// <returns>Success or failure response.</returns>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Invalid or missing user token." });
        }

        var result = await _authService.ChangePasswordAsync(userId, dto);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}
