using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.TrainingSubmission;

/// <summary>
/// DTO for the admin's review decision on a single training deliverable.
/// </summary>
public class DeliverableReviewDecisionDto
{
    /// <summary>
    /// The deliverable being reviewed: "Certificate", "Report", "Presentation",
    /// "CompanyEvaluation", or "StudentSurvey".
    /// </summary>
    [Required]
    public string DeliverableType { get; set; } = string.Empty;

    /// <summary>
    /// The review decision: "Approved" or "Rejected".
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Reason for rejection (required when Status is "Rejected").
    /// </summary>
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
}

/// <summary>
/// DTO for the Training Unit admin reviewing the training deliverables of a submission.
/// </summary>
public class ReviewTrainingDeliverablesDto
{
    /// <summary>
    /// Per-file review decisions. Files not included keep their current status.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<DeliverableReviewDecisionDto> Decisions { get; set; } = new();

    /// <summary>
    /// Optional general feedback from the admin.
    /// </summary>
    [MaxLength(1000)]
    public string? AdminNotes { get; set; }
}
