using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.CompanyProfile;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for company profile management.
/// </summary>
[ApiController]
[Route("api/companies")]
[Authorize(Roles = "Company")]
public class CompanyProfileController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompanyProfileController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    /// <summary>
    /// Creates or updates the company profile for the authenticated user.
    /// </summary>
    /// <param name="dto">The company profile data.</param>
    /// <returns>The company ID.</returns>
    [HttpPost("profile")]
    public async Task<ActionResult<ServiceResponse<int>>> CreateOrUpdateProfile([FromBody] CreateCompanyProfileDto dto)
    {
        // Extract UserId from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ServiceResponse<int>.Failure("Invalid or missing user token."));
        }

        var result = await _companyService.CreateOrUpdateProfileAsync(userId, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the company profile for the authenticated user.
    /// </summary>
    /// <returns>The company profile.</returns>
    [HttpGet("profile")]
    public async Task<ActionResult<ServiceResponse<CompanyDto>>> GetProfile()
    {
        // Extract UserId from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ServiceResponse<CompanyDto>.Failure("Invalid or missing user token."));
        }

        var result = await _companyService.GetProfileAsync(userId);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates the company profile for the authenticated user.
    /// </summary>
    /// <param name="dto">The updated profile data.</param>
    /// <returns>The updated company ID.</returns>
    [HttpPut("profile")]
    public async Task<ActionResult<ServiceResponse<int>>> UpdateProfile([FromBody] CompanyProfileUpdateDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ServiceResponse<int>.Failure("Invalid or missing user token."));
        }

        var result = await _companyService.UpdateCompanyProfileAsync(userId, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Searches for companies based on the provided criteria.
    /// </summary>
    /// <param name="searchDto">The search criteria.</param>
    /// <returns>A paged list of company search results.</returns>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceResponse<PagedResult<CompanySearchResultDto>>>> Search([FromQuery] CompanySearchDto searchDto)
    {
        var result = await _companyService.SearchCompaniesAsync(searchDto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets a company profile by company ID.
    /// </summary>
    /// <param name="id">The company ID.</param>
    /// <returns>The company profile.</returns>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceResponse<CompanyDto>>> GetProfileById(int id)
    {
        var result = await _companyService.GetProfileByCompanyIdAsync(id);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}

