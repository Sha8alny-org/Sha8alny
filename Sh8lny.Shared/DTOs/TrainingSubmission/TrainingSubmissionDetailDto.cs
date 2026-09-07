namespace Sh8lny.Shared.DTOs.TrainingSubmission;

/// <summary>
/// DTO for a single deliverable file in the training submission detail.
/// </summary>
public class TrainingDeliverableFileDto
{
    public string FileType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Detailed response DTO for a training submission, including the per-file breakdown
/// of the 5 deliverables (URL, status, rejection reason) and the overall workflow status.
/// </summary>
public class TrainingSubmissionDetailDto
{
    public int TrainingSubmissionID { get; set; }
    public int ApplicationID { get; set; }
    public int StudentID { get; set; }
    public string? StudentName { get; set; }

    // Legacy URL columns (kept in sync with the per-file records)
    public string? CertificateUrl { get; set; }
    public string? ReportUrl { get; set; }
    public string? PresentationUrl { get; set; }
    public string? CompanyEvaluationUrl { get; set; }
    public string? StudentSurveyUrl { get; set; }

    public string Status { get; set; } = string.Empty;
    public bool IsAdminApproved { get; set; }
    public bool IsCompanyVerified { get; set; }
    public int? TrainingDays { get; set; }
    public int? ApprovedDuration { get; set; }
    public bool IsExternalTraining { get; set; }

    // ── Duration info ──────────────────────────────────────

    /// <summary>
    /// Duration unit of the training opportunity ("Days" or "Hours").
    /// </summary>
    public string? DurationType { get; set; }

    /// <summary>
    /// Equivalent days calculated from the project listing
    /// (hours converted at a 6-hour standard training day).
    /// </summary>
    public int? CalculatedDays { get; set; }

    /// <summary>
    /// The final effective credited days:
    /// admin-approved override first, then declared training days,
    /// then the calculated equivalent days from the project listing.
    /// </summary>
    public int? CreditedDays { get; set; }

    public string? AdminNotes { get; set; }
    public string? RejectionReason { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public DateTime? AdminReviewedAt { get; set; }
    public DateTime? CompanyVerifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Per-file breakdown of the 5 training deliverables.
    /// </summary>
    public List<TrainingDeliverableFileDto> Files { get; set; } = new();
}
