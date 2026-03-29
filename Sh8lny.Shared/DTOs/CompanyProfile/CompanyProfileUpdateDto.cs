namespace Sh8lny.Shared.DTOs.CompanyProfile;

public class CompanyProfileUpdateDto
{
    public string? CompanyName { get; set; }
    public string? Description { get; set; }
    public string? Industry { get; set; }
    public string? WebsiteUrl { get; set; }
    
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    
    public string? LogoUrl { get; set; }
}
