using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.TrainingSubmission;

/// <summary>
/// DTO for submitting training documents by a student.
/// All URLs should be obtained from /api/Media upload endpoints.
/// </summary>
public class SubmitTrainingDocumentsDto
{
    /// <summary>
    /// The application ID this training submission is for.
    /// </summary>
    [Required]
    public int ApplicationID { get; set; }

    /// <summary>
    /// URL to the training certificate document.
    /// </summary>
    [MaxLength(500)]
    public string? CertificateUrl { get; set; }

    /// <summary>
    /// URL to the detailed training report.
    /// </summary>
    [MaxLength(500)]
    public string? ReportUrl { get; set; }

    /// <summary>
    /// URL to the presentation document.
    /// </summary>
    [MaxLength(500)]
    public string? PresentationUrl { get; set; }

    /// <summary>
    /// URL to the company evaluation form.
    /// </summary>
    [MaxLength(500)]
    public string? CompanyEvaluationUrl { get; set; }

    /// <summary>
    /// URL to the student field training survey.
    /// </summary>
    [MaxLength(500)]
    public string? StudentSurveyUrl { get; set; }

    /// <summary>
    /// Number of training days to request credit for.
    /// </summary>
    public int? TrainingDays { get; set; }
}
