using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.MasterData;

namespace Sh8lny.Service;

public class MasterDataService : IMasterDataService
{
    private readonly IUnitOfWork _unitOfWork;

    public MasterDataService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResponse<IEnumerable<SkillDto>>> GetSkillsAsync()
    {
        var skills = await _unitOfWork.Skills.GetAllAsync();
        return ServiceResponse<IEnumerable<SkillDto>>.Success(skills.Select(MapSkill).ToList());
    }

    public async Task<ServiceResponse<SkillDto>> CreateSkillAsync(CreateSkillDto dto)
    {
        var categoryParsed = Enum.TryParse<SkillCategory>(dto.Category, true, out var category);
        var entity = new Skill
        {
            SkillName = dto.Name,
            SkillCategory = categoryParsed ? category : null,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Skills.AddAsync(entity);
        await _unitOfWork.SaveAsync();
        return ServiceResponse<SkillDto>.Success(MapSkill(entity));
    }

    public async Task<ServiceResponse<SkillDto>> UpdateSkillAsync(int id, UpdateSkillDto dto)
    {
        var entity = await _unitOfWork.Skills.GetByIdAsync(id);
        if (entity is null)
            return ServiceResponse<SkillDto>.Failure("Skill not found.");

        var categoryParsed = Enum.TryParse<SkillCategory>(dto.Category, true, out var category);
        entity.SkillName = dto.Name;
        entity.SkillCategory = categoryParsed ? category : null;
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;

        _unitOfWork.Skills.Update(entity);
        await _unitOfWork.SaveAsync();
        return ServiceResponse<SkillDto>.Success(MapSkill(entity));
    }

    public async Task<ServiceResponse<bool>> DeleteSkillAsync(int id)
    {
        var entity = await _unitOfWork.Skills.GetByIdAsync(id);
        if (entity is null)
            return ServiceResponse<bool>.Failure("Skill not found.");

        _unitOfWork.Skills.Remove(entity);
        await _unitOfWork.SaveAsync();
        return ServiceResponse<bool>.Success(true);
    }

    public async Task<ServiceResponse<IEnumerable<DepartmentDto>>> GetDepartmentsAsync()
    {
        var departments = await _unitOfWork.Departments.GetAllAsync();
        return ServiceResponse<IEnumerable<DepartmentDto>>.Success(departments.Select(MapDepartment).ToList());
    }

    public async Task<ServiceResponse<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentDto dto)
    {
        var university = await _unitOfWork.Universities.GetByIdAsync(dto.UniversityId);
        if (university is null)
            return ServiceResponse<DepartmentDto>.Failure("University not found.");

        var entity = new Department
        {
            UniversityID = dto.UniversityId,
            DepartmentName = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Departments.AddAsync(entity);
        await _unitOfWork.SaveAsync();
        return ServiceResponse<DepartmentDto>.Success(MapDepartment(entity));
    }

    public async Task<ServiceResponse<DepartmentDto>> UpdateDepartmentAsync(int id, UpdateDepartmentDto dto)
    {
        var entity = await _unitOfWork.Departments.GetByIdAsync(id);
        if (entity is null)
            return ServiceResponse<DepartmentDto>.Failure("Department not found.");

        var university = await _unitOfWork.Universities.GetByIdAsync(dto.UniversityId);
        if (university is null)
            return ServiceResponse<DepartmentDto>.Failure("University not found.");

        entity.UniversityID = dto.UniversityId;
        entity.DepartmentName = dto.Name;
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;

        _unitOfWork.Departments.Update(entity);
        await _unitOfWork.SaveAsync();
        return ServiceResponse<DepartmentDto>.Success(MapDepartment(entity));
    }

    public async Task<ServiceResponse<bool>> DeleteDepartmentAsync(int id)
    {
        var entity = await _unitOfWork.Departments.GetByIdAsync(id);
        if (entity is null)
            return ServiceResponse<bool>.Failure("Department not found.");

        _unitOfWork.Departments.Remove(entity);
        await _unitOfWork.SaveAsync();
        return ServiceResponse<bool>.Success(true);
    }

    public async Task<ServiceResponse<IEnumerable<UniversityDto>>> GetUniversitiesAsync()
    {
        var universities = await _unitOfWork.Universities.GetAllAsync();
        return ServiceResponse<IEnumerable<UniversityDto>>.Success(universities.Select(MapUniversity).ToList());
    }

    public async Task<ServiceResponse<UniversityDto>> CreateUniversityAsync(CreateUniversityDto dto)
    {
        var typeParsed = Enum.TryParse<UniversityType>(dto.Type, true, out var type);
        var entity = new University
        {
            UniversityName = dto.Name,
            UniversityLogo = dto.Logo,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            Website = dto.Website,
            Address = dto.Address,
            City = dto.City,
            Country = dto.Country,
            UniversityType = typeParsed ? type : null,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Universities.AddAsync(entity);
        await _unitOfWork.SaveAsync();
        return ServiceResponse<UniversityDto>.Success(MapUniversity(entity));
    }

    public async Task<ServiceResponse<UniversityDto>> UpdateUniversityAsync(int id, UpdateUniversityDto dto)
    {
        var entity = await _unitOfWork.Universities.GetByIdAsync(id);
        if (entity is null)
            return ServiceResponse<UniversityDto>.Failure("University not found.");

        var typeParsed = Enum.TryParse<UniversityType>(dto.Type, true, out var type);
        entity.UniversityName = dto.Name;
        entity.UniversityLogo = dto.Logo;
        entity.ContactEmail = dto.ContactEmail;
        entity.ContactPhone = dto.ContactPhone;
        entity.Website = dto.Website;
        entity.Address = dto.Address;
        entity.City = dto.City;
        entity.Country = dto.Country;
        entity.UniversityType = typeParsed ? type : null;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Universities.Update(entity);
        await _unitOfWork.SaveAsync();
        return ServiceResponse<UniversityDto>.Success(MapUniversity(entity));
    }

    public async Task<ServiceResponse<bool>> DeleteUniversityAsync(int id)
    {
        var entity = await _unitOfWork.Universities.GetByIdAsync(id);
        if (entity is null)
            return ServiceResponse<bool>.Failure("University not found.");

        _unitOfWork.Universities.Remove(entity);
        await _unitOfWork.SaveAsync();
        return ServiceResponse<bool>.Success(true);
    }

    private static SkillDto MapSkill(Skill skill) => new()
    {
        Id = skill.SkillID,
        Name = skill.SkillName,
        Category = skill.SkillCategory?.ToString(),
        Description = skill.Description,
        IsActive = skill.IsActive
    };

    private static DepartmentDto MapDepartment(Department department) => new()
    {
        Id = department.DepartmentID,
        Name = department.DepartmentName,
        UniversityId = department.UniversityID,
        Description = department.Description,
        IsActive = department.IsActive
    };

    private static UniversityDto MapUniversity(University university) => new()
    {
        Id = university.UniversityID,
        Name = university.UniversityName,
        Logo = university.UniversityLogo,
        City = university.City,
        Country = university.Country,
        Type = university.UniversityType?.ToString(),
        IsActive = university.IsActive
    };
}
