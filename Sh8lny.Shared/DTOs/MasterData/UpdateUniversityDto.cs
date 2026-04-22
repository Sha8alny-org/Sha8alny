using System.ComponentModel.DataAnnotations;

namespace Sh8lny.Shared.DTOs.MasterData;

public class UpdateUniversityDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;

    public string? Logo { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Type { get; set; }
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
}
