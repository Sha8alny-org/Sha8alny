namespace Sh8lny.Shared.DTOs.CompanyProfile;

public class CompanySearchResultDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? LogoUrl { get; set; }
    public decimal AverageRating { get; set; }
    public DateTime CreatedAt { get; set; }
}
