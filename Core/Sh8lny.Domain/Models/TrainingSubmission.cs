using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sh8lny.Domain.Models;

/// <summary>
/// Represents a field training/internship document submission with dual-approval workflow.
/// Students submit training documents, which require both Admin academic approval and Company industry verification.
/// </summary>
public class TrainingSubmission
{
    [Key]
    public int TrainingSubmissionID { get; set; }

    /// <summary>
    /// The application this training submission is associated with.
    /// </summary>
    public int ApplicationID { get; set; }

    /// <summary>
    /// The student who submitted the training documents.
    /// </summary>
    public int StudentID { get; set; }

    /// <summary>
    /// URL to the training certificate document (uploaded via /api/Media).
    /// </summary>
    [MaxLength(500)]
    public string? CertificateUrl { get; set; }

    /// <summary>
    /// URL to the detailed training report (uploaded via /api/Media).
    /// </summary>
    [MaxLength(500)]
    public string? ReportUrl { get; set; }

    /// <summary>
    /// URL to the presentation document (uploaded via /api/Media).
    /// </summary>
    [MaxLength(500)]
    public string? PresentationUrl { get; set; }

    /// <summary>
    /// URL to the company evaluation form (uploaded via /api/Media).
    /// </summary>
    [MaxLength(500)]
    public string? CompanyEvaluationUrl { get; set; }

    /// <summary>
    /// URL to the student field training survey (uploaded via /api/Media).
    /// </summary>
    [MaxLength(500)]
    public string? StudentSurveyUrl { get; set; }

    /// <summary>
    /// Current status of the training submission in the dual-approval workflow.
    /// </summary>
    public TrainingSubmissionStatus Status { get; set; } = TrainingSubmissionStatus.Pending;

    /// <summary>
    /// Whether the admin has approved the academic aspects of the training.
    /// </summary>
    public bool IsAdminApproved { get; set; }

    /// <summary>
    /// Whether the company has verified the industry training completion.
    /// </summary>
    public bool IsCompanyVerified { get; set; }

    /// <summary>
    /// Number of training days to credit upon full completion.
    /// </summary>
    public int? TrainingDays { get; set; }

    /// <summary>
    /// Notes from the admin reviewer.
    /// </summary>
    [MaxLength(1000)]
    public string? AdminNotes { get; set; }

    /// <summary>
    /// Reason for rejection (if applicable).
    /// </summary>
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// UserID of the admin who reviewed the submission.
    /// </summary>
    public int? ReviewedByAdminId { get; set; }

    /// <summary>
    /// When the admin reviewed the submission.
    /// </summary>
    public DateTime? AdminReviewedAt { get; set; }

    /// <summary>
    /// When the company verified the submission.
    /// </summary>
    public DateTime? CompanyVerifiedAt { get; set; }

    /// <summary>
    /// When the submission was fully completed (both approvals received).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the submission was initially created.
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the submission was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey(nameof(ApplicationID))]
    public virtual Application Application { get; set; } = null!;

    [ForeignKey(nameof(StudentID))]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey(nameof(ReviewedByAdminId))]
    public virtual User? ReviewedByAdmin { get; set; }
}

/// <summary>
/// Status of a training submission in the dual-approval workflow.
/// </summary>
public enum TrainingSubmissionStatus
{
    /// <summary>
    /// Initial state - awaiting review.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Admin has approved the academic aspects.
    /// </summary>
    AdminApproved = 1,

    /// <summary>
    /// Company has verified the industry training.
    /// </summary>
    CompanyVerified = 2,

    /// <summary>
    /// Both admin and company have approved - fully completed.
    /// </summary>
    FullyCompleted = 3,

    /// <summary>
    /// Submission was rejected (by either admin or company).
    /// </summary>
    Rejected = 4
}
