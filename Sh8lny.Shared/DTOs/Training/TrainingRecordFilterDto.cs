namespace Sh8lny.Shared.DTOs.Training;

/// <summary>
/// DTO for advanced filtering of student training/internship records.
/// All properties are optional — omit a filter to skip it.
/// </summary>
public class TrainingRecordFilterDto
{
    // ── Filters ────────────────────────────────────────────

    /// <summary>
    /// Filter by company ID.
    /// </summary>
    public int? CompanyId { get; set; }

    /// <summary>
    /// Only return records whose project starts on or after this date.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Only return records whose project ends on or before this date.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by department ID. Null represents 'All Departments'.
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Filter by academic year (0-based enum value, e.g., 0 = FirstYear).
    /// </summary>
    public int? AcademicYear { get; set; }

    /// <summary>
    /// Optional filter for Training vs Internship (e.g., "Training", "Internship").
    /// </summary>
    public string? ProjectType { get; set; }

    // ── Pagination ─────────────────────────────────────────

    /// <summary>
    /// Page number (1-based, default: 1).
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page (default: 10, max: 100).
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Validates and normalizes filter values.
    /// </summary>
    public void Normalize()
    {
        if (PageNumber < 1) PageNumber = 1;
        if (PageSize < 1) PageSize = 10;
        if (PageSize > 100) PageSize = 100;

        ProjectType = ProjectType?.Trim();
    }
}
