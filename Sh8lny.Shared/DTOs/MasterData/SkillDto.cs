namespace Sh8lny.Shared.DTOs.MasterData;

public class SkillDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
