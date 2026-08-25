using System.Linq.Expressions;
using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.StudentProfile;

namespace Sh8lny.Service;

/// <summary>
/// Service for student profile operations.
/// </summary>
public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;

    public StudentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<int>> CreateProfileAsync(int userId, CreateStudentProfileDto dto)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Check if user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResponse<int>.Failure("User not found.");
            }

            // Check if student profile already exists for this user
            var existingStudent = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == userId);
            if (existingStudent is not null)
            {
                return ServiceResponse<int>.Failure("Student profile already exists for this user.");
            }

            // Parse full name
            var nameParts = dto.FullName.Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

            // Create Student entity
            var student = new Student
            {
                UserID = userId,
                FirstName = firstName,
                LastName = lastName,
                Phone = dto.Phone,
                ProfilePicture = dto.ProfilePicture,
                GitHubProfile = dto.GitHubProfile,
                CvFileUrl = dto.CvFileUrl,
                UniversityID = dto.UniversityID,
                DepartmentID = dto.DepartmentID,
                AcademicYear = dto.AcademicYear.HasValue
                    ? dto.AcademicYear.Value switch
                    {
                        Shared.DTOs.StudentProfile.AcademicYearDto.FirstYear => Domain.Models.AcademicYear.FirstYear,
                        Shared.DTOs.StudentProfile.AcademicYearDto.SecondYear => Domain.Models.AcademicYear.SecondYear,
                        Shared.DTOs.StudentProfile.AcademicYearDto.ThirdYear => Domain.Models.AcademicYear.ThirdYear,
                        Shared.DTOs.StudentProfile.AcademicYearDto.FourthYear => Domain.Models.AcademicYear.FourthYear,
                        Shared.DTOs.StudentProfile.AcademicYearDto.Graduate => Domain.Models.AcademicYear.Graduate,
                        _ => null
                    }
                    : null,
                City = dto.City,
                State = dto.State,
                Country = dto.Country,
                Status = StudentStatus.Active,
                ProfileCompleteness = CalculateProfileCompleteness(dto),
                TotalInternshipDays = dto.TotalInternshipDays,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Auto-link university if UniversityID is null but we have university name in the first education record
            if (student.UniversityID is null && dto.Educations.Count > 0)
            {
                var universityName = dto.Educations[0].UniversityName;
                var match = await _unitOfWork.Universities.FindSingleAsync(u => u.UniversityName == universityName);
                student.UniversityID = match?.UniversityID;
            }

            await _unitOfWork.Students.AddAsync(student);
            await _unitOfWork.SaveAsync();

            // Add Education items
            foreach (var educationDto in dto.Educations)
            {
                var education = new Education
                {
                    StudentID = student.StudentID,
                    UniversityName = educationDto.UniversityName,
                    Degree = educationDto.Degree,
                    FieldOfStudy = educationDto.FieldOfStudy,
                    StartYear = educationDto.StartYear,
                    EndYear = educationDto.EndYear,
                    Description = educationDto.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Educations.AddAsync(education);
            }

            // Add Experience items
            foreach (var experienceDto in dto.Experiences)
            {
                var experience = new Experience
                {
                    StudentID = student.StudentID,
                    CompanyName = experienceDto.CompanyName,
                    Role = experienceDto.Role,
                    Location = experienceDto.Location,
                    StartDate = experienceDto.StartDate,
                    EndDate = experienceDto.EndDate,
                    IsCurrent = experienceDto.IsCurrent,
                    Description = experienceDto.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Experiences.AddAsync(experience);
            }

            // Add StudentSkill items (linking Student to Skills)
            foreach (var skillId in dto.SkillIds.Distinct())
            {
                // Verify skill exists
                var skill = await _unitOfWork.Skills.GetByIdAsync(skillId);
                if (skill is null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<int>.Failure($"Skill with ID {skillId} not found.");
                }

                var studentSkill = new StudentSkill
                {
                    StudentID = student.StudentID,
                    SkillID = skillId,
                    ProficiencyLevel = ProficiencyLevel.Beginner,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.StudentSkills.AddAsync(studentSkill);
            }

            await _unitOfWork.SaveAsync();
            await _unitOfWork.CommitTransactionAsync();

            return ServiceResponse<int>.Success(student.StudentID, "Student profile created successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ServiceResponse<int>.Failure("An error occurred while creating the profile.", 
                new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// Calculates profile completeness percentage.
    /// </summary>
    private static int CalculateProfileCompleteness(CreateStudentProfileDto dto)
    {
        var completeness = 0;
        var totalFields = 8;

        if (!string.IsNullOrWhiteSpace(dto.FullName)) completeness++;
        if (!string.IsNullOrWhiteSpace(dto.Bio)) completeness++;
        if (!string.IsNullOrWhiteSpace(dto.Phone)) completeness++;
        if (!string.IsNullOrWhiteSpace(dto.ProfilePicture)) completeness++;
        if (!string.IsNullOrWhiteSpace(dto.Country)) completeness++;
        if (dto.Educations.Count > 0) completeness++;
        if (dto.Experiences.Count > 0) completeness++;
        if (dto.SkillIds.Count > 0) completeness++;

        return (int)((completeness / (double)totalFields) * 100);
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<int>> UpdateStudentProfileAsync(int userId, StudentProfileUpdateDto dto)
    {
        try
        {
            var student = await _unitOfWork.GetStudentWithSkillsAsync(userId);

            if (student is null)
            {
                return ServiceResponse<int>.Failure("Student profile not found.");
            }

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                student.FirstName = dto.FirstName;

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                student.LastName = dto.LastName;

            if (dto.Bio is not null)
                student.Bio = dto.Bio;

            if (dto.Phone is not null)
                student.Phone = dto.Phone;

            if (dto.ProfilePicture is not null)
                student.ProfilePicture = dto.ProfilePicture;

            if (dto.GitHubProfile is not null)
                student.GitHubProfile = dto.GitHubProfile;

            if (dto.CvFileUrl is not null)
                student.CvFileUrl = dto.CvFileUrl;

            if (dto.UniversityID.HasValue)
                student.UniversityID = dto.UniversityID;

            if (dto.DepartmentID.HasValue)
                student.DepartmentID = dto.DepartmentID;

            if (dto.AcademicYear.HasValue)
                student.AcademicYear = dto.AcademicYear.Value switch
                {
                    Shared.DTOs.StudentProfile.AcademicYearDto.FirstYear => Domain.Models.AcademicYear.FirstYear,
                    Shared.DTOs.StudentProfile.AcademicYearDto.SecondYear => Domain.Models.AcademicYear.SecondYear,
                    Shared.DTOs.StudentProfile.AcademicYearDto.ThirdYear => Domain.Models.AcademicYear.ThirdYear,
                    Shared.DTOs.StudentProfile.AcademicYearDto.FourthYear => Domain.Models.AcademicYear.FourthYear,
                    Shared.DTOs.StudentProfile.AcademicYearDto.Graduate => Domain.Models.AcademicYear.Graduate,
                    _ => student.AcademicYear
                };

            if (dto.StudentIDNumber is not null)
                student.StudentIDNumber = dto.StudentIDNumber;

            if (dto.City is not null)
                student.City = dto.City;

            if (dto.State is not null)
                student.State = dto.State;

            if (dto.Country is not null)
                student.Country = dto.Country;

            // Handle Skills Update
            if (dto.SkillIds.Count > 0)
            {
                var existingSkills = student.StudentSkills ?? new List<StudentSkill>();
                var existingSkillIds = existingSkills.Select(ss => ss.SkillID).ToList();
                var newSkillIds = dto.SkillIds.Distinct().ToList();

                // Remove skills that are not in the new list
                var skillsToRemove = existingSkills
                    .Where(ss => !newSkillIds.Contains(ss.SkillID))
                    .ToList();
                
                foreach (var skill in skillsToRemove)
                {
                    _unitOfWork.StudentSkills.Remove(skill);
                }

                // Add new skills that were not in the existing list
                var skillsToAdd = newSkillIds
                    .Where(id => !existingSkillIds.Contains(id))
                    .ToList();

                foreach (var skillId in skillsToAdd)
                {
                    var skill = await _unitOfWork.Skills.GetByIdAsync(skillId);
                    if (skill is not null)
                    {
                        var studentSkill = new StudentSkill
                        {
                            StudentID = student.StudentID,
                            SkillID = skillId,
                            ProficiencyLevel = ProficiencyLevel.Beginner,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.StudentSkills.AddAsync(studentSkill);
                    }
                }
            }

            student.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Students.Update(student);
            await _unitOfWork.SaveAsync();

            return ServiceResponse<int>.Success(student.StudentID, "Student profile updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<int>.Failure("An error occurred while updating the profile.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<PagedResult<StudentSearchResultDto>>> SearchStudentsAsync(StudentSearchDto searchDto)
    {
        try
        {
            Expression<Func<Student, bool>>? predicate = null;

            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                var keyword = searchDto.Keyword.ToLower();
                predicate = s =>
                    (s.FirstName + " " + s.LastName).ToLower().Contains(keyword) ||
                    s.FirstName.ToLower().Contains(keyword) ||
                    s.LastName.ToLower().Contains(keyword) ||
                    (s.Bio != null && s.Bio.ToLower().Contains(keyword));
            }

            IEnumerable<Student> students;
            
            if (predicate != null)
            {
                students = await _unitOfWork.Students.FindAsync(predicate);
            }
            else
            {
                students = await _unitOfWork.Students.GetAllAsync();
            }

            var query = students.AsQueryable();

            if (searchDto.DepartmentID.HasValue)
            {
                query = query.Where(s => s.DepartmentID == searchDto.DepartmentID);
            }

            if (searchDto.UniversityID.HasValue)
            {
                query = query.Where(s => s.UniversityID == searchDto.UniversityID);
            }

            if (!string.IsNullOrWhiteSpace(searchDto.AcademicYear))
            {
                query = query.Where(s => s.AcademicYear != null && s.AcademicYear.ToString() == searchDto.AcademicYear);
            }

            if (!string.IsNullOrWhiteSpace(searchDto.City))
            {
                query = query.Where(s => s.City != null && s.City.ToLower() == searchDto.City.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(searchDto.Country))
            {
                query = query.Where(s => s.Country != null && s.Country.ToLower() == searchDto.Country.ToLower());
            }

            var totalCount = query.Count();

            var skip = (searchDto.PageNumber - 1) * searchDto.PageSize;
            var result = query
                .OrderByDescending(s => s.CreatedAt)
                .Skip(skip)
                .Take(searchDto.PageSize)
                .Select(s => new StudentSearchResultDto
                {
                    Id = s.StudentID,
                    Name = s.FirstName + " " + s.LastName,
                    StudyYear = s.AcademicYear != null ? s.AcademicYear.ToString() : string.Empty,
                    JoinDate = s.CreatedAt,
                    Department = s.Department != null ? s.Department.DepartmentName : string.Empty
                })
                .ToList();

            var pagedResult = new PagedResult<StudentSearchResultDto>
            {
                Items = result,
                TotalCount = totalCount,
                PageNumber = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };

            return ServiceResponse<PagedResult<StudentSearchResultDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return ServiceResponse<PagedResult<StudentSearchResultDto>>.Failure("An error occurred while searching for students.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<StudentResponseDto>> GetProfileAsync(int userId)
    {
        try
        {
            var student = await _unitOfWork.GetStudentWithSkillsAsync(userId);

            if (student is null)
            {
                return ServiceResponse<StudentResponseDto>.Failure("Student profile not found.");
            }

            // Safely map skills with null checks
            var skills = student.StudentSkills?.Select(ss => new Shared.DTOs.StudentProfile.StudentSkillDto
            {
                Id = ss.SkillID,
                Name = ss.Skill?.SkillName ?? string.Empty,
                Category = ss.Skill?.SkillCategory?.ToString(),
                Description = ss.Skill?.Description,
                IsActive = ss.Skill?.IsActive ?? false
            }).ToList() ?? new List<Shared.DTOs.StudentProfile.StudentSkillDto>();

            var studentDto = new StudentResponseDto
            {
                Id = student.StudentID,
                UserId = student.UserID,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Bio = student.Bio,
                Phone = student.Phone,
                ProfilePicture = student.ProfilePicture,
                GitHubProfile = student.GitHubProfile,
                UniversityID = student.UniversityID,
                UniversityName = student.University?.UniversityName,
                DepartmentID = student.DepartmentID,
                DepartmentName = student.Department?.DepartmentName,
                AcademicYear = student.AcademicYear?.ToString(),
                StudentIDNumber = student.StudentIDNumber,
                City = student.City,
                State = student.State,
                Country = student.Country,
                ProfileCompleteness = student.ProfileCompleteness,
                Status = student.Status.ToString(),
                AverageRating = student.AverageRating,
                TotalReviews = student.TotalReviews,
                TotalInternshipDays = student.TotalInternshipDays,
                CvFileUrl = student.CvFileUrl,
                Skills = skills,
                Educations = (student.Educations ?? new List<Education>())
                    .Select(e => new EducationDto
                    {
                        Id = e.EducationID,
                        UniversityName = e.UniversityName,
                        Degree = e.Degree,
                        FieldOfStudy = e.FieldOfStudy,
                        StartYear = e.StartYear,
                        EndYear = e.EndYear,
                        Description = e.Description
                    })
                    .ToList(),
                Experiences = (student.Experiences ?? new List<Experience>())
                    .Select(ex => new ExperienceDto
                    {
                        Id = ex.ExperienceID,
                        CompanyName = ex.CompanyName,
                        Role = ex.Role,
                        Location = ex.Location,
                        StartDate = ex.StartDate,
                        EndDate = ex.EndDate,
                        IsCurrent = ex.IsCurrent,
                        Description = ex.Description
                    })
                    .ToList(),
                CreatedAt = student.CreatedAt,
                UpdatedAt = student.UpdatedAt
            };

            return ServiceResponse<StudentResponseDto>.Success(studentDto);
        }
        catch (Exception ex)
        {
            return ServiceResponse<StudentResponseDto>.Failure("An error occurred while retrieving the student profile.",
                new List<string> { ex.Message });
        }
    }
}
