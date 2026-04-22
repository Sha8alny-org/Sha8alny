using System.ComponentModel.DataAnnotations;
using Sh8lny.Shared.Validation;

namespace Sh8lny.Shared.DTOs.StudentProfile;

/// <summary>
/// DTO for creating a complete student profile.
/// </summary>
public class CreateStudentProfileDto
{
    // Personal Info
    [Required]
    public required string FullName { get; set; }
    public string? Bio { get; set; }
    public string? Phone { get; set; }
    public string? ProfilePicture { get; set; }
    public string? GitHubProfile { get; set; }

    [Required]
    [AllowedFileExtensions(".pdf", ".docx", ".pptx")]
    public string CvFileUrl { get; set; } = string.Empty;
    
    // Location
    [Required]
    public string? City { get; set; }
    public string? State { get; set; }
    [Required]
    public required string Country { get; set; }

    // Education & Experience
    [Required]
    [MinLength(1, ErrorMessage = "At least one education entry is required.")]
    public List<EducationDto> Educations { get; set; } = new();

    [Required]
    [MinLength(1, ErrorMessage = "At least one experience is required.")]
    public List<ExperienceDto> Experiences { get; set; } = new();

    // Skills (list of Skill IDs to link)
    public List<int> SkillIds { get; set; } = new();

    // Internship Days (optional initial value)
    public int TotalInternshipDays { get; set; } = 0;
}
