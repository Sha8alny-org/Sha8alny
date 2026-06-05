using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Repositories;
using Sh8lny.Shared.DTOs.Auth;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for cross-cutting user operations (search, etc.).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Searches across all user types (students and companies) by name or email.
    /// Used by the mobile app to find contacts for starting a new chat.
    /// </summary>
    /// <param name="query">Partial name or email to search for.</param>
    /// <param name="excludeSelf">If true, excludes the calling user from results (default: true).</param>
    /// <returns>Top 20 matching users.</returns>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserSearchResultDto>>> SearchUsers(
        [FromQuery] string query,
        [FromQuery] bool excludeSelf = true)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return BadRequest(new { Message = "Search query must be at least 2 characters." });
        }

        var currentUserId = GetCurrentUserId();

        var searchTerm = query.Trim().ToLower();
        var results = new List<UserSearchResultDto>();

        // Search students by first/last name
        var students = await _unitOfWork.Students.FindAsync(s =>
            (s.FirstName.ToLower().Contains(searchTerm) ||
             s.LastName.ToLower().Contains(searchTerm)));

        foreach (var student in students)
        {
            if (excludeSelf && currentUserId.HasValue && student.UserID == currentUserId.Value)
                continue;

            results.Add(new UserSearchResultDto
            {
                UserId = student.UserID,
                FullName = student.FullName,
                UserType = "Student",
                ProfilePictureUrl = student.ProfilePicture
            });
        }

        // Search companies by company name
        var companies = await _unitOfWork.Companies.FindAsync(c =>
            c.CompanyName.ToLower().Contains(searchTerm));

        foreach (var company in companies)
        {
            if (excludeSelf && currentUserId.HasValue && company.UserID == currentUserId.Value)
                continue;

            results.Add(new UserSearchResultDto
            {
                UserId = company.UserID,
                FullName = company.CompanyName,
                UserType = "Company",
                ProfilePictureUrl = company.CompanyLogo
            });
        }

        // Also search by email on the User table
        var users = await _unitOfWork.Users.FindAsync(u =>
            u.Email.ToLower().Contains(searchTerm));

        foreach (var user in users)
        {
            if (excludeSelf && currentUserId.HasValue && user.UserID == currentUserId.Value)
                continue;

            // Avoid duplicates (already added via student/company search)
            if (results.Any(r => r.UserId == user.UserID))
                continue;

            var name = user.FirstName is not null && user.LastName is not null
                ? $"{user.FirstName} {user.LastName}".Trim()
                : user.Email;

            results.Add(new UserSearchResultDto
            {
                UserId = user.UserID,
                FullName = name,
                UserType = user.UserType.ToString(),
                ProfilePictureUrl = null
            });
        }

        // Return top 20 results
        return Ok(results.Take(20).ToList());
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}
