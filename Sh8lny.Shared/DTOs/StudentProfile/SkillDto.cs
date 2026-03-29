namespace Sh8lny.Shared.DTOs.StudentProfile;

public class StudentSkillDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
