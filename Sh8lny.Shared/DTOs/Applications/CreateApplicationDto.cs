using System.ComponentModel.DataAnnotations;
using Sh8lny.Shared.Validation;

namespace Sh8lny.Shared.DTOs.Applications;

/// <summary>
/// DTO for creating a new application.
/// </summary>
public class CreateApplicationDto
{
    public int ProjectId { get; set; }

    [Required]
    [AllowedFileExtensions(".pdf", ".docx", ".pptx")]
    public string ProposalFileUrl { get; set; } = string.Empty;

    [Required]
    [AllowedFileExtensions(".pdf", ".docx", ".pptx")]
    public string StudentCvUrl { get; set; } = string.Empty;

    public decimal BidAmount { get; set; }
}
