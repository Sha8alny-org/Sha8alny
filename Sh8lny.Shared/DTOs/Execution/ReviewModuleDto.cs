using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.Execution;

public class ReviewModuleDto
{
    [Required]
    public string Status { get; set; } = string.Empty;

    public string? CompanyFeedback { get; set; }
}
