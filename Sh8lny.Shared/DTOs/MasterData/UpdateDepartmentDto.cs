using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.MasterData;

public class UpdateDepartmentDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int UniversityId { get; set; }

    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
