using System.Linq.Expressions;
using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.CompanyProfile;

namespace Sh8lny.Service;

/// <summary>
/// Service for company profile operations.
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<int>> CreateOrUpdateProfileAsync(int userId, CreateCompanyProfileDto dto)
    {
        try
        {
            // Check if user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResponse<int>.Failure("User not found.");
            }

            // Check if company profile already exists for this user
            var existingCompany = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == userId);

            if (existingCompany is not null)
            {
                // Update existing company
                existingCompany.CompanyName = dto.CompanyName;
                existingCompany.Description = dto.Description;
                existingCompany.Industry = dto.Industry;
                existingCompany.Website = dto.WebsiteUrl;
                existingCompany.Address = dto.Address;
                existingCompany.City = dto.City;
                existingCompany.State = dto.State;
                existingCompany.Country = dto.Country;
                existingCompany.ContactEmail = dto.ContactEmail;
                existingCompany.ContactPhone = dto.ContactPhone;
                existingCompany.CompanyLogo = dto.LogoUrl;
                existingCompany.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Companies.Update(existingCompany);
                await _unitOfWork.SaveAsync();

                return ServiceResponse<int>.Success(existingCompany.CompanyID, "Company profile updated successfully.");
            }
            else
            {
                // Create new company
                var company = new Company
                {
                    UserID = userId,
                    CompanyName = dto.CompanyName,
                    Description = dto.Description,
                    Industry = dto.Industry,
                    Website = dto.WebsiteUrl,
                    Address = dto.Address,
                    City = dto.City,
                    State = dto.State,
                    Country = dto.Country,
                    ContactEmail = dto.ContactEmail,
                    ContactPhone = dto.ContactPhone,
                    CompanyLogo = dto.LogoUrl,
                    AverageRating = 0,
                    TotalReviews = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Companies.AddAsync(company);
                await _unitOfWork.SaveAsync();

                return ServiceResponse<int>.Success(company.CompanyID, "Company profile created successfully.");
            }
        }
        catch (Exception ex)
        {
            return ServiceResponse<int>.Failure("An error occurred while saving the company profile.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<CompanyDto>> GetProfileAsync(int userId)
    {
        try
        {
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == userId);

            if (company is null)
            {
                return ServiceResponse<CompanyDto>.Failure("Company profile not found.");
            }

            var companyDto = new CompanyDto
            {
                Id = company.CompanyID,
                UserId = company.UserID,
                CompanyName = company.CompanyName,
                Description = company.Description,
                Industry = company.Industry,
                LogoUrl = company.CompanyLogo,
                ContactEmail = company.ContactEmail,
                ContactPhone = company.ContactPhone,
                WebsiteUrl = company.Website,
                Address = company.Address,
                City = company.City,
                State = company.State,
                Country = company.Country,
                AverageRating = company.AverageRating,
                TotalReviews = company.TotalReviews,
                CreatedAt = company.CreatedAt,
                UpdatedAt = company.UpdatedAt
            };

            return ServiceResponse<CompanyDto>.Success(companyDto);
        }
        catch (Exception ex)
        {
            return ServiceResponse<CompanyDto>.Failure("An error occurred while retrieving the company profile.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<int>> UpdateCompanyProfileAsync(int userId, CompanyProfileUpdateDto dto)
    {
        try
        {
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == userId);
            if (company is null)
            {
                return ServiceResponse<int>.Failure("Company profile not found.");
            }

            if (!string.IsNullOrWhiteSpace(dto.CompanyName))
                company.CompanyName = dto.CompanyName;

            if (dto.Description is not null)
                company.Description = dto.Description;

            if (dto.Industry is not null)
                company.Industry = dto.Industry;

            if (dto.WebsiteUrl is not null)
                company.Website = dto.WebsiteUrl;

            if (dto.Address is not null)
                company.Address = dto.Address;

            if (dto.City is not null)
                company.City = dto.City;

            if (dto.State is not null)
                company.State = dto.State;

            if (dto.Country is not null)
                company.Country = dto.Country;

            if (dto.ContactEmail is not null)
                company.ContactEmail = dto.ContactEmail;

            if (dto.ContactPhone is not null)
                company.ContactPhone = dto.ContactPhone;

            if (dto.LogoUrl is not null)
                company.CompanyLogo = dto.LogoUrl;

            company.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Companies.Update(company);
            await _unitOfWork.SaveAsync();

            return ServiceResponse<int>.Success(company.CompanyID, "Company profile updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<int>.Failure("An error occurred while updating the profile.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<PagedResult<CompanySearchResultDto>>> SearchCompaniesAsync(CompanySearchDto searchDto)
    {
        try
        {
            Expression<Func<Company, bool>>? predicate = null;

            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                var keyword = searchDto.Keyword.ToLower();
                predicate = c =>
                    c.CompanyName.ToLower().Contains(keyword) ||
                    (c.Description != null && c.Description.ToLower().Contains(keyword));
            }

            IEnumerable<Company> companies;

            if (predicate != null)
            {
                companies = await _unitOfWork.Companies.FindAsync(predicate);
            }
            else
            {
                companies = await _unitOfWork.Companies.GetAllAsync();
            }

            var query = companies.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchDto.Industry))
            {
                query = query.Where(c => c.Industry != null && c.Industry.ToLower() == searchDto.Industry.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(searchDto.City))
            {
                query = query.Where(c => c.City != null && c.City.ToLower() == searchDto.City.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(searchDto.Country))
            {
                query = query.Where(c => c.Country != null && c.Country.ToLower() == searchDto.Country.ToLower());
            }

            var totalCount = query.Count();

            var skip = (searchDto.PageNumber - 1) * searchDto.PageSize;
            var result = query
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(searchDto.PageSize)
                .Select(c => new CompanySearchResultDto
                {
                    Id = c.CompanyID,
                    CompanyName = c.CompanyName,
                    Industry = c.Industry,
                    City = c.City,
                    Country = c.Country,
                    LogoUrl = c.CompanyLogo,
                    AverageRating = c.AverageRating,
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            var pagedResult = new PagedResult<CompanySearchResultDto>
            {
                Items = result,
                TotalCount = totalCount,
                PageNumber = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };

            return ServiceResponse<PagedResult<CompanySearchResultDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return ServiceResponse<PagedResult<CompanySearchResultDto>>.Failure("An error occurred while searching for companies.",
                new List<string> { ex.Message });
        }
    }
}
