using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.MasterData;

public class CreateSkillDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
