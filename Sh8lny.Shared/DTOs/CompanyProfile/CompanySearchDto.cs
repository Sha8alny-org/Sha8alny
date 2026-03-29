namespace Sh8lny.Shared.DTOs.CompanyProfile;

public class CompanySearchDto
{
    public string? Keyword { get; set; }
    public string? Industry { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
