namespace Sh8lny.Shared.DTOs.Applications;

/// <summary>
/// DTO for viewing applicants of a project ranked by GPA.
/// </summary>
public class RankedApplicantDto
{
    public int ApplicationId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentTitle { get; set; }
    public decimal? Gpa { get; set; }
    public string? UniversityName { get; set; }
    public string? DepartmentName { get; set; }
    public string? AcademicYear { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }
}
