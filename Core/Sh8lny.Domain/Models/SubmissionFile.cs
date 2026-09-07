using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sh8lny.Domain.Models;

/// <summary>
/// Represents an individual file attached to a training submission.
/// Supports per-file approval/rejection and re-upload workflows.
/// File binaries are uploaded via /api/Media; only the resulting URL string is stored.
/// </summary>
public class SubmissionFile
{
    [Key]
    public int SubmissionFileID { get; set; }

    /// <summary>
    /// The training submission this file belongs to.
    /// </summary>
    public int TrainingSubmissionID { get; set; }

    /// <summary>
    /// The type of document this file represents.
    /// </summary>
    public SubmissionFileType FileType { get; set; } = SubmissionFileType.Other;

    /// <summary>
    /// URL to the uploaded file (uploaded via /api/Media).
    /// </summary>
    [MaxLength(500)]
    public required string FileUrl { get; set; }

    /// <summary>
    /// Current review status of this file.
    /// </summary>
    public SubmissionFileStatus Status { get; set; } = SubmissionFileStatus.Pending;

    /// <summary>
    /// Reason for rejection (if applicable).
    /// </summary>
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// When the file was uploaded.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the file was last updated (e.g., re-uploaded or reviewed).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(TrainingSubmissionID))]
    public virtual TrainingSubmission TrainingSubmission { get; set; } = null!;
}

/// <summary>
/// Type of a submission file.
/// </summary>
public enum SubmissionFileType
{
    /// <summary>
    /// Presentation document.
    /// </summary>
    Presentation = 0,

    /// <summary>
    /// Detailed training report.
    /// </summary>
    Report = 1,

    /// <summary>
    /// Student evaluation form.
    /// </summary>
    StudentEvaluation = 2,

    /// <summary>
    /// Training certificate document.
    /// </summary>
    Certificate = 3,

    /// <summary>
    /// Any other document type.
    /// </summary>
    Other = 4
}

/// <summary>
/// Review status of a submission file.
/// </summary>
public enum SubmissionFileStatus
{
    /// <summary>
    /// Awaiting review.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Reviewed and approved.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Reviewed and rejected (see RejectionReason).
    /// </summary>
    Rejected = 2
}
