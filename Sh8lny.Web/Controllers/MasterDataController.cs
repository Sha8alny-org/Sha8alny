using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.MasterData;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for retrieving master/lookup data for dropdowns.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MasterDataController : ControllerBase
{
    private readonly IMasterDataService _masterDataService;

    public MasterDataController(IMasterDataService masterDataService)
    {
        _masterDataService = masterDataService;
    }

    /// <summary>
    /// Gets all skills for dropdown selection.
    /// </summary>
    /// <returns>List of skills.</returns>
    [HttpGet("skills")]
    public async Task<ActionResult<IEnumerable<SkillDto>>> GetSkills()
    {
        var result = await _masterDataService.GetSkillsAsync();
        return Ok(result.Data);
    }

    [HttpPost("skills")]
    public async Task<ActionResult<ServiceResponse<SkillDto>>> CreateSkill([FromBody] CreateSkillDto dto)
    {
        var result = await _masterDataService.CreateSkillAsync(dto);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("skills/{id:int}")]
    public async Task<ActionResult<ServiceResponse<SkillDto>>> UpdateSkill(int id, [FromBody] UpdateSkillDto dto)
    {
        var result = await _masterDataService.UpdateSkillAsync(id, dto);
        if (!result.IsSuccess) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("skills/{id:int}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSkill(int id)
    {
        var result = await _masterDataService.DeleteSkillAsync(id);
        if (!result.IsSuccess) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Gets all departments for dropdown selection.
    /// </summary>
    /// <returns>List of departments.</returns>
    [HttpGet("departments")]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
    {
        var result = await _masterDataService.GetDepartmentsAsync();
        return Ok(result.Data);
    }

    [HttpPost("departments")]
    public async Task<ActionResult<ServiceResponse<DepartmentDto>>> CreateDepartment([FromBody] CreateDepartmentDto dto)
    {
        var result = await _masterDataService.CreateDepartmentAsync(dto);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("departments/{id:int}")]
    public async Task<ActionResult<ServiceResponse<DepartmentDto>>> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto dto)
    {
        var result = await _masterDataService.UpdateDepartmentAsync(id, dto);
        if (!result.IsSuccess) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("departments/{id:int}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteDepartment(int id)
    {
        var result = await _masterDataService.DeleteDepartmentAsync(id);
        if (!result.IsSuccess) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Gets all universities for dropdown selection.
    /// </summary>
    /// <returns>List of universities.</returns>
    [HttpGet("universities")]
    public async Task<ActionResult<IEnumerable<UniversityDto>>> GetUniversities()
    {
        var result = await _masterDataService.GetUniversitiesAsync();
        return Ok(result.Data);
    }

    [HttpPost("universities")]
    public async Task<ActionResult<ServiceResponse<UniversityDto>>> CreateUniversity([FromBody] CreateUniversityDto dto)
    {
        var result = await _masterDataService.CreateUniversityAsync(dto);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("universities/{id:int}")]
    public async Task<ActionResult<ServiceResponse<UniversityDto>>> UpdateUniversity(int id, [FromBody] UpdateUniversityDto dto)
    {
        var result = await _masterDataService.UpdateUniversityAsync(id, dto);
        if (!result.IsSuccess) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("universities/{id:int}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteUniversity(int id)
    {
        var result = await _masterDataService.DeleteUniversityAsync(id);
        if (!result.IsSuccess) return NotFound(result);
        return Ok(result);
    }
}
