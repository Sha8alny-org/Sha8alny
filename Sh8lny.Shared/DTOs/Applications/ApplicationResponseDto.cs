namespace Sh8lny.Shared.DTOs.Applications;

/// <summary>
/// Response DTO for application details (student view).
/// </summary>
public class ApplicationResponseDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }
    public decimal BidAmount { get; set; }
    public string? StudentCvUrl { get; set; }
}
