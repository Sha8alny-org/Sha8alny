using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.TrainingSubmission;

/// <summary>
/// DTO for a student submitting the 5 training deliverable documents.
/// All URLs should be obtained from /api/Media upload endpoints.
/// </summary>
public class SubmitTrainingDeliverablesDto
{
    /// <summary>
    /// The application ID this training submission is for.
    /// </summary>
    [Required]
    public int ApplicationId { get; set; }

    /// <summary>
    /// URL to the training certificate document.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string CertificateUrl { get; set; }

    /// <summary>
    /// URL to the detailed training report.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string ReportUrl { get; set; }

    /// <summary>
    /// URL to the presentation document.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string PresentationUrl { get; set; }

    /// <summary>
    /// URL to the company evaluation form.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string CompanyEvaluationUrl { get; set; }

    /// <summary>
    /// URL to the student field training survey.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string StudentSurveyUrl { get; set; }

    /// <summary>
    /// Number of training days to request credit for.
    /// </summary>
    public int? TrainingDays { get; set; }
}
