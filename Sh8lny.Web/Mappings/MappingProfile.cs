using AutoMapper;
using Sh8lny.Domain.Models;
using Sh8lny.Web.DTOs.MasterData;
using Sh8lny.Web.DTOs.Projects;
using Sh8lny.Shared.DTOs.StudentProfile;

namespace Sh8lny.Web.Mappings;

/// <summary>
/// AutoMapper profile containing all mapping configurations.
/// Add your entity-to-DTO mappings here.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Project mappings
        CreateMap<Project, ProjectResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ProjectID))
            .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyID))
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company.CompanyName))
            .ForMember(dest => dest.ProjectType, opt => opt.MapFrom(src => src.ProjectType.HasValue ? src.ProjectType.Value.ToString() : null))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        // Master Data mappings
        CreateMap<Skill, Sh8lny.Web.DTOs.MasterData.SkillDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.SkillID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.SkillName))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.SkillCategory.HasValue ? src.SkillCategory.Value.ToString() : null));

        CreateMap<Department, DepartmentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.DepartmentID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.DepartmentName))
            .ForMember(dest => dest.UniversityId, opt => opt.MapFrom(src => src.UniversityID));

        CreateMap<University, UniversityDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UniversityID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.UniversityName))
            .ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.UniversityLogo))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.UniversityType.HasValue ? src.UniversityType.Value.ToString() : null));

        // Student mappings - manually mapped in service layer
        CreateMap<Student, StudentResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.StudentID))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserID))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName))
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : null))
            .ForMember(dest => dest.AcademicYear, opt => opt.MapFrom(src => src.AcademicYear.HasValue ? src.AcademicYear.Value.ToString() : null))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        // StudentSkill to StudentSkillDto mapping
        CreateMap<Skill, StudentSkillDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.SkillID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.SkillName))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.SkillCategory.HasValue ? src.SkillCategory.Value.ToString() : null));

        // Add more mappings below as needed
    }
}
