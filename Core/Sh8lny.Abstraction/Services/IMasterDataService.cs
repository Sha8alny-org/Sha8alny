using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.MasterData;

namespace Sh8lny.Abstraction.Services;

public interface IMasterDataService
{
    Task<ServiceResponse<IEnumerable<SkillDto>>> GetSkillsAsync();
    Task<ServiceResponse<SkillDto>> CreateSkillAsync(CreateSkillDto dto);
    Task<ServiceResponse<SkillDto>> UpdateSkillAsync(int id, UpdateSkillDto dto);
    Task<ServiceResponse<bool>> DeleteSkillAsync(int id);

    Task<ServiceResponse<IEnumerable<DepartmentDto>>> GetDepartmentsAsync();
    Task<ServiceResponse<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentDto dto);
    Task<ServiceResponse<DepartmentDto>> UpdateDepartmentAsync(int id, UpdateDepartmentDto dto);
    Task<ServiceResponse<bool>> DeleteDepartmentAsync(int id);

    Task<ServiceResponse<IEnumerable<UniversityDto>>> GetUniversitiesAsync();
    Task<ServiceResponse<UniversityDto>> CreateUniversityAsync(CreateUniversityDto dto);
    Task<ServiceResponse<UniversityDto>> UpdateUniversityAsync(int id, UpdateUniversityDto dto);
    Task<ServiceResponse<bool>> DeleteUniversityAsync(int id);
}
