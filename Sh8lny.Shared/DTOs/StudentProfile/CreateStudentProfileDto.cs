namespace Sh8lny.Shared.DTOs.StudentProfile;

/// <summary>
/// DTO for creating a complete student profile.
/// </summary>
public class CreateStudentProfileDto
{
    // Personal Info
    public required string FullName { get; set; }
    public string? Bio { get; set; }
    public string? Phone { get; set; }
    public string? ProfilePicture { get; set; }
    public string? GitHubProfile { get; set; }
    
    // Location
    public string? City { get; set; }
    public string? State { get; set; }
    public required string Country { get; set; }

    // Education & Experience
    public List<EducationDto> Educations { get; set; } = new();
    public List<ExperienceDto> Experiences { get; set; } = new();

    // Skills (list of Skill IDs to link)
    public List<int> SkillIds { get; set; } = new();

    // Internship Days (optional initial value)
    public int TotalInternshipDays { get; set; } = 0;
}
