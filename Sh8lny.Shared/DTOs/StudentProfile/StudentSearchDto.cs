namespace Sh8lny.Shared.DTOs.StudentProfile;

public class StudentSearchDto
{
    public string? Keyword { get; set; }
    public int? DepartmentID { get; set; }
    public int? UniversityID { get; set; }
    public string? AcademicYear { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
