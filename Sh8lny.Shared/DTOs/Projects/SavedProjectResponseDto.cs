namespace Sh8lny.Shared.DTOs.Projects;

public class SavedProjectResponseDto
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ProjectType { get; set; }
    public string? CompanyName { get; set; }
    public DateTime Deadline { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; }
}
