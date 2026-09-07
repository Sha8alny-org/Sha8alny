using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.TrainingSubmission;

/// <summary>
/// DTO for the Training Unit admin overriding the final approved training duration (in days).
/// Used when the actual certificate proves a duration different from the project's original listing.
/// </summary>
public class OverrideTrainingDurationDto
{
    /// <summary>
    /// The final approved duration in days.
    /// </summary>
    [Required]
    [Range(1, 365, ErrorMessage = "Duration must be between 1 and 365 days.")]
    public int ApprovedDuration { get; set; }

    /// <summary>
    /// Optional justification for changing the duration (e.g. "Certificate proves 7 days instead of 10").
    /// </summary>
    [MaxLength(1000)]
    public string? AdminNotes { get; set; }
}
