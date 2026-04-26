using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Projects;

namespace Sh8lny.Service;

/// <summary>
/// Service for project/opportunity operations.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProjectService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<int>> CreateProjectAsync(int userId, CreateProjectDto dto)
    {
        try
        {
            // Verify user exists and is a company
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResponse<int>.Failure("User not found.");
            }

            if (user.UserType != UserType.Company)
            {
                return ServiceResponse<int>.Failure("Only companies can create projects.");
            }

            // Get company profile
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == userId);
            if (company is null)
            {
                return ServiceResponse<int>.Failure("Company profile not found. Please create a company profile first.");
            }

            // Validate deadline
            if (dto.Deadline < DateTime.UtcNow)
            {
                return ServiceResponse<int>.Failure("Deadline must be in the future.");
            }

            // Validate dates if provided
            if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
            {
                return ServiceResponse<int>.Failure("End date must be after start date.");
            }

            // Parse project type
            ProjectType? projectType = null;
            if (!string.IsNullOrWhiteSpace(dto.ProjectType) && 
                Enum.TryParse<ProjectType>(dto.ProjectType, ignoreCase: true, out var parsedType))
            {
                projectType = parsedType;
            }

            await _unitOfWork.BeginTransactionAsync();

            // Create project
            var project = new Project
            {
                CompanyID = company.CompanyID,
                ProjectName = dto.Title,
                Description = dto.Description,
                ProjectType = projectType,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Deadline = dto.Deadline,
                Duration = dto.Duration,
                MinAcademicYear = dto.MinAcademicYear,
                MaxApplicants = dto.MaxApplicants,
                Status = ProjectStatus.Active,
                IsVisible = dto.IsVisible,
                CreatedBy = userId,
                CreatedByName = company.CompanyName,
                ViewCount = 0,
                ApplicationCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Projects.AddAsync(project);
            await _unitOfWork.SaveAsync();

            // Add required skills
            foreach (var skillId in dto.RequiredSkillIds.Distinct())
            {
                var skill = await _unitOfWork.Skills.GetByIdAsync(skillId);
                if (skill is null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<int>.Failure($"Skill with ID {skillId} not found.");
                }

                var projectSkill = new ProjectRequiredSkill
                {
                    ProjectID = project.ProjectID,
                    SkillID = skillId,
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.ProjectRequiredSkills.AddAsync(projectSkill);
            }

            await _unitOfWork.SaveAsync();
            await _unitOfWork.CommitTransactionAsync();

            return ServiceResponse<int>.Success(project.ProjectID, "Project created successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ServiceResponse<int>.Failure("An error occurred while creating the project.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> UpdateProjectAsync(int userId, int projectId, UpdateProjectDto dto)
    {
        try
        {
            // Get project
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project is null)
            {
                return ServiceResponse<bool>.Failure("Project not found.");
            }

            // Verify ownership
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == userId);
            if (company is null || project.CompanyID != company.CompanyID)
            {
                return ServiceResponse<bool>.Failure("You do not have permission to update this project.");
            }

            // Validate dates if provided
            if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
            {
                return ServiceResponse<bool>.Failure("End date must be after start date.");
            }

            // Parse project type
            ProjectType? projectType = null;
            if (!string.IsNullOrWhiteSpace(dto.ProjectType) && 
                Enum.TryParse<ProjectType>(dto.ProjectType, ignoreCase: true, out var parsedType))
            {
                projectType = parsedType;
            }

            // Parse status if provided
            if (!string.IsNullOrWhiteSpace(dto.Status) && 
                Enum.TryParse<ProjectStatus>(dto.Status, ignoreCase: true, out var parsedStatus))
            {
                project.Status = parsedStatus;
            }

            await _unitOfWork.BeginTransactionAsync();

            // Update project fields
            project.ProjectName = dto.Title;
            project.Description = dto.Description;
            project.ProjectType = projectType;
            project.StartDate = dto.StartDate;
            project.EndDate = dto.EndDate;
            project.Deadline = dto.Deadline;
            project.Duration = dto.Duration;
            project.MinAcademicYear = dto.MinAcademicYear;
            project.MaxApplicants = dto.MaxApplicants;
            project.IsVisible = dto.IsVisible;
            project.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Projects.Update(project);

            // Update required skills - remove existing and add new
            var existingSkills = await _unitOfWork.ProjectRequiredSkills.FindAsync(ps => ps.ProjectID == projectId);
            foreach (var existingSkill in existingSkills)
            {
                _unitOfWork.ProjectRequiredSkills.Remove(existingSkill);
            }

            foreach (var skillId in dto.RequiredSkillIds.Distinct())
            {
                var skill = await _unitOfWork.Skills.GetByIdAsync(skillId);
                if (skill is null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<bool>.Failure($"Skill with ID {skillId} not found.");
                }

                var projectSkill = new ProjectRequiredSkill
                {
                    ProjectID = projectId,
                    SkillID = skillId,
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.ProjectRequiredSkills.AddAsync(projectSkill);
            }

            await _unitOfWork.SaveAsync();
            await _unitOfWork.CommitTransactionAsync();

            return ServiceResponse<bool>.Success(true, "Project updated successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ServiceResponse<bool>.Failure("An error occurred while updating the project.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> DeleteProjectAsync(int userId, int projectId)
    {
        try
        {
            // Get project
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project is null)
            {
                return ServiceResponse<bool>.Failure("Project not found.");
            }

            // Verify ownership
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == userId);
            if (company is null || project.CompanyID != company.CompanyID)
            {
                return ServiceResponse<bool>.Failure("You do not have permission to delete this project.");
            }

            await _unitOfWork.BeginTransactionAsync();

            // Remove required skills first
            var existingSkills = await _unitOfWork.ProjectRequiredSkills.FindAsync(ps => ps.ProjectID == projectId);
            foreach (var skill in existingSkills)
            {
                _unitOfWork.ProjectRequiredSkills.Remove(skill);
            }

            // Remove project
            _unitOfWork.Projects.Remove(project);

            await _unitOfWork.SaveAsync();
            await _unitOfWork.CommitTransactionAsync();

            return ServiceResponse<bool>.Success(true, "Project deleted successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ServiceResponse<bool>.Failure("An error occurred while deleting the project.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<ProjectResponseDto>> GetProjectByIdAsync(int projectId)
    {
        try
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project is null)
            {
                return ServiceResponse<ProjectResponseDto>.Failure("Project not found.");
            }

            // Get company info
            var company = await _unitOfWork.Companies.GetByIdAsync(project.CompanyID);

            // Get required skills
            var projectSkills = await _unitOfWork.ProjectRequiredSkills.FindAsync(ps => ps.ProjectID == projectId);
            var skillDtos = new List<ProjectSkillDto>();

            foreach (var ps in projectSkills)
            {
                var skill = await _unitOfWork.Skills.GetByIdAsync(ps.SkillID);
                if (skill is not null)
                {
                    skillDtos.Add(new ProjectSkillDto
                    {
                        Id = skill.SkillID,
                        Name = skill.SkillName,
                        IsRequired = ps.IsRequired
                    });
                }
            }

            // Increment view count
            project.ViewCount++;
            _unitOfWork.Projects.Update(project);
            await _unitOfWork.SaveAsync();

            var dto = MapToResponseDto(project, company, skillDtos);
            return ServiceResponse<ProjectResponseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProjectResponseDto>.Failure("An error occurred while retrieving the project.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<IEnumerable<ProjectResponseDto>>> GetCompanyProjectsAsync(int userId)
    {
        try
        {
            // Get company
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == userId);
            if (company is null)
            {
                return ServiceResponse<IEnumerable<ProjectResponseDto>>.Failure("Company profile not found.");
            }

            var projects = await _unitOfWork.Projects.FindAsync(p => p.CompanyID == company.CompanyID);
            var projectDtos = new List<ProjectResponseDto>();

            foreach (var project in projects)
            {
                // Get required skills for each project
                var projectSkills = await _unitOfWork.ProjectRequiredSkills.FindAsync(ps => ps.ProjectID == project.ProjectID);
                var skillDtos = new List<ProjectSkillDto>();

                foreach (var ps in projectSkills)
                {
                    var skill = await _unitOfWork.Skills.GetByIdAsync(ps.SkillID);
                    if (skill is not null)
                    {
                        skillDtos.Add(new ProjectSkillDto
                        {
                            Id = skill.SkillID,
                            Name = skill.SkillName,
                            IsRequired = ps.IsRequired
                        });
                    }
                }

                projectDtos.Add(MapToResponseDto(project, company, skillDtos));
            }

            return ServiceResponse<IEnumerable<ProjectResponseDto>>.Success(projectDtos);
        }
        catch (Exception ex)
        {
            return ServiceResponse<IEnumerable<ProjectResponseDto>>.Failure("An error occurred while retrieving projects.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<PagedResult<ProjectResponseDto>>> GetFilteredProjectsAsync(ProjectFilterDto filter)
    {
        try
        {
            // ── Normalize ──────────────────────────────────────
            filter.Normalize();

            // Get all projects (GenericRepository doesn't expose IQueryable)
            var allProjects = await _unitOfWork.Projects.GetAllAsync();
            var query = allProjects.AsEnumerable();

            // ── 1. Visibility ──────────────────────────────────
            if (filter.IsVisible.HasValue)
            {
                query = query.Where(p => p.IsVisible == filter.IsVisible.Value);
            }

            // ── 2. Keyword (Title OR Description) ──────────────
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.ToLowerInvariant();
                query = query.Where(p =>
                    p.ProjectName.ToLowerInvariant().Contains(kw) ||
                    p.Description.ToLowerInvariant().Contains(kw));
            }

            // ── 3. Project Type ─────────────────────────────────
            if (!string.IsNullOrWhiteSpace(filter.ProjectType) &&
                Enum.TryParse<ProjectType>(filter.ProjectType, ignoreCase: true, out var projectType))
            {
                query = query.Where(p => p.ProjectType == projectType);
            }

            // ── 4. Status ───────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(filter.Status) &&
                Enum.TryParse<ProjectStatus>(filter.Status, ignoreCase: true, out var status))
            {
                query = query.Where(p => p.Status == status);
            }

            // ── 5. Company ──────────────────────────────────────
            if (filter.CompanyId.HasValue)
            {
                query = query.Where(p => p.CompanyID == filter.CompanyId.Value);
            }

            // ── 6. Deadline window ──────────────────────────────
            if (filter.DeadlineAfter.HasValue)
            {
                query = query.Where(p => p.Deadline >= filter.DeadlineAfter.Value);
            }

            if (filter.DeadlineBefore.HasValue)
            {
                query = query.Where(p => p.Deadline <= filter.DeadlineBefore.Value);
            }

            // ── 7. Skills (at-least-one match) ──────────────────
            if (filter.SkillIds is { Count: > 0 })
            {
                var requestedSkillIds = filter.SkillIds.ToHashSet();

                // Fetch all project-skill mappings that match any of the requested skill IDs
                var matchingMappings = await _unitOfWork.ProjectRequiredSkills
                    .FindAsync(ps => requestedSkillIds.Contains(ps.SkillID));

                var projectIdsWithSkill = matchingMappings
                    .Select(ps => ps.ProjectID)
                    .ToHashSet();

                query = query.Where(p => projectIdsWithSkill.Contains(p.ProjectID));
            }

            // ── 8. Sorting (preset-based) ───────────────────────
            query = ApplySorting(query, filter.SortBy);

            // ── 9. Materialize & count ──────────────────────────
            var projectList = query.ToList();
            var totalCount = projectList.Count;

            // ── 10. Pagination ──────────────────────────────────
            var pagedProjects = projectList
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            // ── 11. Map to DTOs (with skills & company info) ────
            var projectDtos = new List<ProjectResponseDto>();
            foreach (var project in pagedProjects)
            {
                var company = await _unitOfWork.Companies.GetByIdAsync(project.CompanyID);
                var projectSkills = await _unitOfWork.ProjectRequiredSkills
                    .FindAsync(ps => ps.ProjectID == project.ProjectID);

                var skillDtos = new List<ProjectSkillDto>();
                foreach (var ps in projectSkills)
                {
                    var skill = await _unitOfWork.Skills.GetByIdAsync(ps.SkillID);
                    if (skill is not null)
                    {
                        skillDtos.Add(new ProjectSkillDto
                        {
                            Id = skill.SkillID,
                            Name = skill.SkillName,
                            IsRequired = ps.IsRequired
                        });
                    }
                }

                projectDtos.Add(MapToResponseDto(project, company, skillDtos));
            }

            var pagedResult = PagedResult<ProjectResponseDto>.Create(
                projectDtos,
                filter.PageNumber,
                filter.PageSize,
                totalCount);

            return ServiceResponse<PagedResult<ProjectResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return ServiceResponse<PagedResult<ProjectResponseDto>>.Failure(
                "An error occurred while searching projects.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> ToggleSaveProjectAsync(int userId, int projectId)
    {
        try
        {
            var student = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == userId);
            if (student is null)
            {
                return ServiceResponse<bool>.Failure("Student profile not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project is null)
            {
                return ServiceResponse<bool>.Failure("Project not found.");
            }

            var existing = await _unitOfWork.SavedOpportunities
                .FindSingleAsync(s => s.StudentID == student.StudentID && s.ProjectID == projectId);

            if (existing is not null)
            {
                _unitOfWork.SavedOpportunities.Remove(existing);
                await _unitOfWork.SaveAsync();
                return ServiceResponse<bool>.Success(false, "Project removed from saved list.");
            }

            await _unitOfWork.SavedOpportunities.AddAsync(new SavedOpportunity
            {
                StudentID = student.StudentID,
                ProjectID = projectId,
                SavedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveAsync();
            return ServiceResponse<bool>.Success(true, "Project saved successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Failure(
                "An error occurred while toggling saved project.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<IEnumerable<SavedProjectResponseDto>>> GetSavedProjectsAsync(int userId)
    {
        try
        {
            var student = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == userId);
            if (student is null)
            {
                return ServiceResponse<IEnumerable<SavedProjectResponseDto>>.Failure("Student profile not found.");
            }

            var saved = await _unitOfWork.GetSavedOpportunitiesWithProjectAsync(student.StudentID);

            var result = saved
                .Where(s => s.Project is not null)
                .Select(s => new SavedProjectResponseDto
                {
                    ProjectId = s.ProjectID,
                    Title = s.Project.ProjectName,
                    Description = s.Project.Description,
                    ProjectType = s.Project.ProjectType?.ToString(),
                    CompanyName = s.Project.Company?.CompanyName,
                    Deadline = s.Project.Deadline,
                    Status = s.Project.Status.ToString(),
                    SavedAt = s.SavedAt
                })
                .ToList();

            return ServiceResponse<IEnumerable<SavedProjectResponseDto>>.Success(result);
        }
        catch (Exception ex)
        {
            return ServiceResponse<IEnumerable<SavedProjectResponseDto>>.Failure(
                "An error occurred while retrieving saved projects.",
                new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// Applies preset-based sorting to the project collection.
    /// Supported presets: newest, oldest, deadline_asc, deadline_desc,
    /// views_desc, applications_desc, title_asc, title_desc.
    /// </summary>
    private static IEnumerable<Project> ApplySorting(IEnumerable<Project> projects, string? sortBy)
    {
        return sortBy switch
        {
            "oldest"             => projects.OrderBy(p => p.CreatedAt),
            "deadline_asc"       => projects.OrderBy(p => p.Deadline),
            "deadline_desc"      => projects.OrderByDescending(p => p.Deadline),
            "views_desc"         => projects.OrderByDescending(p => p.ViewCount),
            "applications_desc"  => projects.OrderByDescending(p => p.ApplicationCount),
            "title_asc"          => projects.OrderBy(p => p.ProjectName),
            "title_desc"         => projects.OrderByDescending(p => p.ProjectName),
            _                    => projects.OrderByDescending(p => p.CreatedAt) // "newest" (default)
        };
    }

    /// <summary>
    /// Maps a Project entity to ProjectResponseDto.
    /// </summary>
    private static ProjectResponseDto MapToResponseDto(Project project, Company? company, List<ProjectSkillDto> skills)
    {
        return new ProjectResponseDto
        {
            Id = project.ProjectID,
            CompanyId = project.CompanyID,
            CompanyName = company?.CompanyName,
            CompanyLogo = company?.CompanyLogo,
            Title = project.ProjectName,
            ProjectCode = project.ProjectCode,
            Description = project.Description,
            ProjectType = project.ProjectType?.ToString(),
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Deadline = project.Deadline,
            Duration = project.Duration,
            RequiredSkills = skills,
            MinAcademicYear = project.MinAcademicYear,
            MaxApplicants = project.MaxApplicants,
            Status = project.Status.ToString(),
            IsVisible = project.IsVisible,
            CreatedBy = project.CreatedBy,
            CreatedByName = project.CreatedByName,
            ViewCount = project.ViewCount,
            ApplicationCount = project.ApplicationCount,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }
}
