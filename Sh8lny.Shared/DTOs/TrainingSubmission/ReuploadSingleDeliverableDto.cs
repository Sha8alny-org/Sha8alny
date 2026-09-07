using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.TrainingSubmission;

/// <summary>
/// DTO for a student re-uploading a single rejected training deliverable.
/// Only rejected deliverables can be re-uploaded; approved ones are preserved.
/// </summary>
public class ReuploadSingleDeliverableDto
{
    /// <summary>
    /// The deliverable to re-upload: "Certificate", "Report", "Presentation",
    /// "CompanyEvaluation", or "StudentSurvey".
    /// </summary>
    [Required]
    public string DeliverableType { get; set; } = string.Empty;

    /// <summary>
    /// The new file URL (obtained from /api/Media upload endpoint).
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string NewUrl { get; set; }
}
