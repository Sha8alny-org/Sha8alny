using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.CompanyProfile;

namespace Sh8lny.Abstraction.Services;

/// <summary>
/// Interface for company profile operations.
/// </summary>
public interface ICompanyService
{
    /// <summary>
    /// Creates a new company profile or updates an existing one.
    /// </summary>
    /// <param name="userId">The ID of the user creating/updating the profile.</param>
    /// <param name="dto">The company profile data.</param>
    /// <returns>Service response containing the company ID.</returns>
    Task<ServiceResponse<int>> CreateOrUpdateProfileAsync(int userId, CreateCompanyProfileDto dto);

    /// <summary>
    /// Gets the company profile for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>Service response containing the company profile.</returns>
    Task<ServiceResponse<CompanyDto>> GetProfileAsync(int userId);

    /// <summary>
    /// Updates the company profile for the authenticated user.
    /// </summary>
    /// <param name="userId">The ID of the user updating the profile.</param>
    /// <param name="dto">The updated profile data.</param>
    /// <returns>Service response containing the updated company ID.</returns>
    Task<ServiceResponse<int>> UpdateCompanyProfileAsync(int userId, CompanyProfileUpdateDto dto);

    /// <summary>
    /// Searches for companies based on the provided criteria.
    /// </summary>
    /// <param name="searchDto">The search criteria.</param>
    /// <returns>Service response containing a paged list of company search results.</returns>
    Task<ServiceResponse<PagedResult<CompanySearchResultDto>>> SearchCompaniesAsync(CompanySearchDto searchDto);

    /// <summary>
    /// Gets the company profile by company ID.
    /// </summary>
    /// <param name="companyId">The ID of the company.</param>
    /// <returns>Service response containing the company profile.</returns>
    Task<ServiceResponse<CompanyDto>> GetProfileByCompanyIdAsync(int companyId);
}

