namespace Sh8lny.Shared.DTOs.StudentProfile;

public class StudentProfileUpdateDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? Phone { get; set; }
    public string? ProfilePicture { get; set; }
    public string? GitHubProfile { get; set; }
    
    public int? UniversityID { get; set; }
    public int? DepartmentID { get; set; }
    public AcademicYearDto? AcademicYear { get; set; }
    public string? StudentIDNumber { get; set; }

    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }

    // Skills (list of Skill IDs to update)
    public List<int>? SkillIds { get; set; }
}

public enum AcademicYearDto
{
    FirstYear,
    SecondYear,
    ThirdYear,
    FourthYear,
    Graduate
}
