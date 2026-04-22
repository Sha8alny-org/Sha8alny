namespace Sh8lny.Shared.DTOs.MasterData;

public class UniversityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Type { get; set; }
    public bool IsActive { get; set; }
}
