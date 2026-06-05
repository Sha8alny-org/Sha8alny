using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.TrainingSubmission;

/// <summary>
/// DTO for admin review of a training submission.
/// </summary>
public class AdminReviewTrainingDto
{
    /// <summary>
    /// Whether the admin approves the training submission.
    /// </summary>
    [Required]
    public bool IsApproved { get; set; }

    /// <summary>
    /// Admin notes about the review.
    /// </summary>
    [MaxLength(1000)]
    public string? AdminNotes { get; set; }

    /// <summary>
    /// Reason for rejection (required if IsApproved is false).
    /// </summary>
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
}
