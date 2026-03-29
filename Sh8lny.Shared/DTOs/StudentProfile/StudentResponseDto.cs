namespace Sh8lny.Shared.DTOs.StudentProfile;

public class StudentResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Bio { get; set; }
    public string? Phone { get; set; }
    public string? ProfilePicture { get; set; }
    public string? GitHubProfile { get; set; }
    
    public int? UniversityID { get; set; }
    public string? UniversityName { get; set; }
    public int? DepartmentID { get; set; }
    public string? DepartmentName { get; set; }
    public string? AcademicYear { get; set; }
    public string? StudentIDNumber { get; set; }

    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }

    public int ProfileCompleteness { get; set; }
    public string? Status { get; set; }

    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalInternshipDays { get; set; }

    public List<StudentSkillDto> Skills { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
