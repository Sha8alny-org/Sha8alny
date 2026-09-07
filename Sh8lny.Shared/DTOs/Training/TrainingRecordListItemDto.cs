namespace Sh8lny.Shared.DTOs.Training;

/// <summary>
/// DTO for a single student training/internship record in the advanced search results.
/// Combines student details, training (project) details, and submission status.
/// </summary>
public class TrainingRecordListItemDto
{
    // ── Student details ────────────────────────────────────

    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? DepartmentName { get; set; }
    public string? UniversityName { get; set; }
    public decimal? Gpa { get; set; }

    // ── Training details ───────────────────────────────────

    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? ProjectType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Duration { get; set; }

    // ── Submission & status info ───────────────────────────

    public string SubmissionStatus { get; set; } = string.Empty;
    public int? TrainingDays { get; set; }
    public int? ApprovedDuration { get; set; }
}
