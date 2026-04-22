namespace Sh8lny.Shared.DTOs.MasterData;

public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UniversityId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
