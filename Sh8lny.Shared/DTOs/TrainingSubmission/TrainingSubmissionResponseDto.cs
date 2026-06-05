namespace Sh8lny.Shared.DTOs.TrainingSubmission;

/// <summary>
/// Response DTO for training submission details.
/// </summary>
public class TrainingSubmissionResponseDto
{
    public int TrainingSubmissionID { get; set; }
    public int ApplicationID { get; set; }
    public int StudentID { get; set; }
    public string? StudentName { get; set; }
    public string? CertificateUrl { get; set; }
    public string? ReportUrl { get; set; }
    public string? PresentationUrl { get; set; }
    public string? CompanyEvaluationUrl { get; set; }
    public string? StudentSurveyUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsAdminApproved { get; set; }
    public bool IsCompanyVerified { get; set; }
    public int? TrainingDays { get; set; }
    public string? AdminNotes { get; set; }
    public string? RejectionReason { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public string? AdminReviewerName { get; set; }
    public DateTime? AdminReviewedAt { get; set; }
    public DateTime? CompanyVerifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
