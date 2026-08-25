using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Auth;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.Options;
using BC = BCrypt.Net.BCrypt;

namespace Sh8lny.Service;

/// <summary>
/// Service for handling user authentication (registration and login).
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtOptions _jwtOptions;
    private readonly IMailService _mailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> jwtOptions,
        IMailService mailService,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtOptions = jwtOptions.Value;
        _mailService = mailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // Check if email already exists
        var existingUser = await _unitOfWork.Users.FindSingleAsync(u => u.Email == dto.Email);
        if (existingUser is not null)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Email already registered."
            };
        }

        // Parse and validate role
        if (!Enum.TryParse<UserType>(dto.Role, ignoreCase: true, out var userType))
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid role specified."
            };
        }

        // Hash password using BCrypt
        var passwordHash = BC.HashPassword(dto.Password);

        // Create new user
        var user = new User
        {
            Email = dto.Email,
            PasswordHash = passwordHash,
            UserType = userType,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveAsync();

        // Generate JWT token
        var (token, expiration) = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            IsSuccess = true,
            Token = token,
            Expiration = expiration,
            UserId = user.UserID,
            Email = user.Email,
            Role = user.UserType.ToString(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Message = "Registration successful."
        };
    }

    /// <inheritdoc />
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // Find user by email
        var user = await _unitOfWork.Users.FindSingleAsync(u => u.Email == dto.Email);
        if (user is null)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password."
            };
        }

        // Verify password using BCrypt
        if (!BC.Verify(dto.Password, user.PasswordHash))
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password."
            };
        }

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveAsync();

        // Fallback for FirstName/LastName if they are null/empty on User
        var firstName = user.FirstName;
        var lastName = user.LastName;
        if (string.IsNullOrEmpty(firstName))
        {
            if (user.UserType == UserType.Student)
            {
                var student = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == user.UserID);
                firstName = student?.FirstName;
                lastName = student?.LastName;
            }
            else if (user.UserType == UserType.Company)
            {
                var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == user.UserID);
                firstName = company?.CompanyName;
                lastName = string.Empty;
            }
        }

        // Generate JWT token
        var (token, expiration) = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            IsSuccess = true,
            Token = token,
            Expiration = expiration,
            UserId = user.UserID,
            Email = user.Email,
            Role = user.UserType.ToString(),
            FirstName = firstName,
            LastName = lastName,
            Message = "Login successful."
        };
    }

    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    /// <param name="user">The user to generate token for.</param>
    /// <returns>Tuple containing token string and expiration date.</returns>
    private (string Token, DateTime Expiration) GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.UserType.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var expiration = DateTime.UtcNow.AddMinutes(_jwtOptions.DurationInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<UserSummaryDto>> GetCurrentUserAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null)
        {
            return ServiceResponse<UserSummaryDto>.Failure("User not found.");
        }

        var summary = new UserSummaryDto
        {
            Id = user.UserID,
            Email = user.Email,
            Role = user.UserType.ToString()
        };

        switch (user.UserType)
        {
            case UserType.Student:
                var student = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == userId);
                if (student is not null)
                {
                    summary.DisplayName = $"{student.FirstName} {student.LastName}".Trim();
                    summary.ProfilePictureUrl = student.ProfilePicture;
                }
                else
                {
                    summary.DisplayName = user.Email;
                }
                break;

            case UserType.Company:
                var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == userId);
                if (company is not null)
                {
                    summary.DisplayName = company.CompanyName;
                    summary.ProfilePictureUrl = company.CompanyLogo;
                }
                else
                {
                    summary.DisplayName = user.Email;
                }
                break;

            case UserType.Admin:
            case UserType.University:
            default:
                summary.DisplayName = user.FirstName is not null && user.LastName is not null
                    ? $"{user.FirstName} {user.LastName}".Trim()
                    : user.Email;
                break;
        }

        return ServiceResponse<UserSummaryDto>.Success(summary);
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<string>> ForgotPasswordAsync(string email)
    {
        var user = await _unitOfWork.Users.FindSingleAsync(u => u.Email == email);

        // Always return success to prevent email enumeration
        if (user is null)
            return ServiceResponse<string>.Success("If an account with that email exists, a reset code has been sent.");

        // Generate a 6-digit code
        var code = new Random().Next(100000, 999999).ToString();

        user.PasswordResetToken = code;
        user.ResetTokenExpires = DateTime.UtcNow.AddMinutes(15);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveAsync();

        // Build a simple HTML email
        var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 480px; margin: auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                <h2 style='color: #333;'>Password Reset</h2>
                <p>Hello,</p>
                <p>You requested a password reset for your <strong>Sha8alny</strong> account.</p>
                <p>Your verification code is:</p>
                <div style='text-align: center; margin: 24px 0;'>
                    <span style='font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #1a73e8;'>{code}</span>
                </div>
                <p>This code will expire in <strong>15 minutes</strong>.</p>
                <p style='color: #888; font-size: 12px;'>If you did not request this, please ignore this email.</p>
            </div>";

        try
        {
            await _mailService.SendEmailAsync(user.Email, "Sha8alny - Password Reset Code", htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", user.Email);
        }

        return ServiceResponse<string>.Success("If an account with that email exists, a reset code has been sent.");
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<string>> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _unitOfWork.Users.FindSingleAsync(u => u.Email == dto.Email);

        if (user is null)
            return ServiceResponse<string>.Failure("Invalid request.");

        // Validate token and expiry
        if (user.PasswordResetToken != dto.Token || user.ResetTokenExpires == null || user.ResetTokenExpires <= DateTime.UtcNow)
            return ServiceResponse<string>.Failure("Invalid or expired reset code.");

        // Hash the new password and clear the token
        user.PasswordHash = BC.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.ResetTokenExpires = null;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveAsync();

        return ServiceResponse<string>.Success("Password has been reset successfully.");
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> UpdateFcmTokenAsync(int userId, string fcmToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null)
            return ServiceResponse<bool>.Failure("User not found.");

        user.FcmToken = fcmToken;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveAsync();

        return ServiceResponse<bool>.Success(true, "FCM token updated successfully.");
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null)
            return ServiceResponse<bool>.Failure("User not found.");

        // Verify current password
        if (!BC.Verify(dto.CurrentPassword, user.PasswordHash))
            return ServiceResponse<bool>.Failure("Current password is incorrect.");

        // Validate new password confirmation
        if (dto.NewPassword != dto.ConfirmNewPassword)
            return ServiceResponse<bool>.Failure("New password and confirmation do not match.");

        // Hash and save the new password
        user.PasswordHash = BC.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveAsync();

        return ServiceResponse<bool>.Success(true, "Password has been changed successfully.");
    }
}
