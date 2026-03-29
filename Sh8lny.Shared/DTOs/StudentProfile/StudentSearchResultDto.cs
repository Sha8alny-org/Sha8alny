namespace Sh8lny.Shared.DTOs.StudentProfile;

public class StudentSearchResultDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StudyYear { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; }
    public string Department { get; set; } = string.Empty;
}
