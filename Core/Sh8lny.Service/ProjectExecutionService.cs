using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Certificates;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Execution;
using Sh8lny.Shared.DTOs.Notifications;

namespace Sh8lny.Service;

/// <summary>
/// Service for project execution operations (modules and progress tracking).
/// </summary>
public class ProjectExecutionService : IProjectExecutionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotifier _notifier;
    private readonly ICertificateService _certificateService;

    public ProjectExecutionService(IUnitOfWork unitOfWork, INotifier notifier, ICertificateService certificateService)
    {
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _certificateService = certificateService;
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<int>> AddModuleAsync(int companyUserId, int projectId, CreateProjectModuleDto dto)
    {
        try
        {
            // 1. Verify company owns the project
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == companyUserId);
            if (company is null)
            {
                return ServiceResponse<int>.Failure("Company profile not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project is null)
            {
                return ServiceResponse<int>.Failure("Project not found.");
            }

            if (project.CompanyID != company.CompanyID)
            {
                return ServiceResponse<int>.Failure("You do not have permission to modify this project.");
            }

            // 1.5 Verify project type is Internship - modules/milestones only for internships
            if (project.ProjectType != ProjectType.Internship)
            {
                return ServiceResponse<int>.Failure(
                    "Modules/Milestones are only available for Internship projects.");
            }

            // 2. Validate weight - total must not exceed 100%
            var existingModules = await _unitOfWork.ProjectModules.FindAsync(m => m.ProjectId == projectId);
            var totalWeight = existingModules.Sum(m => m.Weight) + dto.Weight;

            if (totalWeight > 100)
            {
                var availableWeight = 100 - existingModules.Sum(m => m.Weight);
                return ServiceResponse<int>.Failure(
                    $"Total module weight cannot exceed 100%. Available weight: {availableWeight}%");
            }

            if (dto.Weight <= 0)
            {
                return ServiceResponse<int>.Failure("Module weight must be greater than 0.");
            }

            // 3. Get next order index
            var maxOrder = existingModules.Any() ? existingModules.Max(m => m.OrderIndex) : 0;

            // 4. Create module
            var module = new ProjectModule
            {
                ProjectId = projectId,
                Title = dto.Name,
                Description = dto.Description,
                Weight = dto.Weight,
                EstimatedDuration = dto.EstimatedDuration,
                OrderIndex = maxOrder + 1,
                Status = ModuleStatus.Pending
            };

            await _unitOfWork.ProjectModules.AddAsync(module);
            await _unitOfWork.SaveAsync();

            return ServiceResponse<int>.Success(module.Id, "Module added successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<int>.Failure("An error occurred while adding the module.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<IEnumerable<ProjectModuleDto>>> GetProjectModulesAsync(int projectId)
    {
        try
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project is null)
            {
                return ServiceResponse<IEnumerable<ProjectModuleDto>>.Failure("Project not found.");
            }

            var modules = await _unitOfWork.ProjectModules.FindAsync(m => m.ProjectId == projectId);

            var dtos = modules
                .OrderBy(m => m.OrderIndex)
                .Select(m => new ProjectModuleDto
                {
                    Id = m.Id,
                    Name = m.Title,
                    Description = m.Description,
                    Weight = m.Weight,
                    Status = m.Status.ToString(),
                    EstimatedDuration = m.EstimatedDuration,
                    OrderIndex = m.OrderIndex
                })
                .ToList();

            return ServiceResponse<IEnumerable<ProjectModuleDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return ServiceResponse<IEnumerable<ProjectModuleDto>>.Failure(
                "An error occurred while retrieving modules.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> UpdateProgressAsync(int studentUserId, int applicationId, UpdateProgressDto dto)
    {
        try
        {
            // 1. Verify student owns the application
            var student = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == studentUserId);
            if (student is null)
            {
                return ServiceResponse<bool>.Failure("Student profile not found.");
            }

            var application = await _unitOfWork.Applications.GetByIdAsync(applicationId);
            if (application is null)
            {
                return ServiceResponse<bool>.Failure("Application not found.");
            }

            if (application.StudentID != student.StudentID)
            {
                return ServiceResponse<bool>.Failure("You do not have permission to update this application's progress.");
            }

            // 2. Verify application status allows progress updates
            if (application.Status != ApplicationStatus.Accepted && 
                application.Status != ApplicationStatus.InProgress)
            {
                return ServiceResponse<bool>.Failure(
                    $"Cannot update progress for application with status '{application.Status}'. Application must be Accepted or In Progress.");
            }

            // 3. Verify module exists and belongs to the application's project
            var module = await _unitOfWork.ProjectModules.GetByIdAsync(dto.ModuleId);
            if (module is null)
            {
                return ServiceResponse<bool>.Failure("Module not found.");
            }

            if (module.ProjectId != application.ProjectID)
            {
                return ServiceResponse<bool>.Failure("Module does not belong to this application's project.");
            }

            // 4. Validate progress percentage
            if (dto.ProgressPercentage < 0 || dto.ProgressPercentage > 100)
            {
                return ServiceResponse<bool>.Failure("Progress percentage must be between 0 and 100.");
            }

            // 5. Update or create progress record
            var progress = await _unitOfWork.ApplicationModuleProgress
                .FindSingleAsync(p => p.ApplicationId == applicationId && p.ProjectModuleId == dto.ModuleId);

            if (progress is null)
            {
                progress = new ApplicationModuleProgress
                {
                    ApplicationId = applicationId,
                    ProjectModuleId = dto.ModuleId,
                    ProgressPercentage = dto.ProgressPercentage,
                    Note = dto.Note,
                    IsCompleted = dto.ProgressPercentage == 100,
                    CompletedAt = dto.ProgressPercentage == 100 ? DateTime.UtcNow : null,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.ApplicationModuleProgress.AddAsync(progress);
            }
            else
            {
                progress.ProgressPercentage = dto.ProgressPercentage;
                progress.Note = dto.Note;
                progress.IsCompleted = dto.ProgressPercentage == 100;
                progress.CompletedAt = dto.ProgressPercentage == 100 ? DateTime.UtcNow : progress.CompletedAt;
                progress.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.ApplicationModuleProgress.Update(progress);
            }

            await _unitOfWork.SaveAsync();

            // 6. Check if all modules are complete - Auto-Completion Logic
            await CheckAndCompleteApplicationAsync(application, student);

            return ServiceResponse<bool>.Success(true, "Progress updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Failure("An error occurred while updating progress.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<ProjectProgressDto>> GetApplicationProgressAsync(int applicationId)
    {
        try
        {
            var application = await _unitOfWork.Applications.GetByIdAsync(applicationId);
            if (application is null)
            {
                return ServiceResponse<ProjectProgressDto>.Failure("Application not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(application.ProjectID);
            if (project is null)
            {
                return ServiceResponse<ProjectProgressDto>.Failure("Project not found.");
            }

            var student = await _unitOfWork.Students.GetByIdAsync(application.StudentID);

            // Get all modules for the project
            var modules = await _unitOfWork.ProjectModules.FindAsync(m => m.ProjectId == application.ProjectID);

            // Get all progress records for this application
            var progressRecords = await _unitOfWork.ApplicationModuleProgress
                .FindAsync(p => p.ApplicationId == applicationId);

            var progressDict = progressRecords.ToDictionary(p => p.ProjectModuleId);

            var moduleProgressList = new List<ModuleProgressDto>();
            decimal overallProgress = 0;
            int completedCount = 0;

            foreach (var module in modules.OrderBy(m => m.OrderIndex))
            {
                var hasProgress = progressDict.TryGetValue(module.Id, out var progressRecord);
                var progressPercentage = hasProgress ? progressRecord!.ProgressPercentage : 0;
                var isCompleted = hasProgress && progressRecord!.IsCompleted;

                if (isCompleted) completedCount++;

                // Calculate weighted progress contribution
                overallProgress += (module.Weight / 100m) * progressPercentage;

                moduleProgressList.Add(new ModuleProgressDto
                {
                    ModuleId = module.Id,
                    ModuleName = module.Title,
                    Weight = module.Weight,
                    ProgressPercentage = progressPercentage,
                    IsCompleted = isCompleted,
                    CompletedAt = hasProgress ? progressRecord!.CompletedAt : null,
                    Note = hasProgress ? progressRecord!.Note : null
                });
            }

            var dto = new ProjectProgressDto
            {
                ApplicationId = applicationId,
                ProjectId = application.ProjectID,
                ProjectName = project.ProjectName,
                StudentName = student?.FullName ?? "Unknown",
                ApplicationStatus = application.Status.ToString(),
                OverallProgress = Math.Round(overallProgress, 2),
                Modules = moduleProgressList,
                CompletedModules = completedCount,
                TotalModules = modules.Count()
            };

            return ServiceResponse<ProjectProgressDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProjectProgressDto>.Failure(
                "An error occurred while retrieving progress.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> ReviewModuleAsync(int companyUserId, int moduleId, ReviewModuleDto dto)
    {
        try
        {
            var module = await _unitOfWork.ProjectModules.GetByIdAsync(moduleId);
            if (module is null)
            {
                return ServiceResponse<bool>.Failure("Module not found.");
            }

            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == companyUserId);
            if (company is null)
            {
                return ServiceResponse<bool>.Failure("Company profile not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(module.ProjectId);
            if (project is null || project.CompanyID != company.CompanyID)
            {
                return ServiceResponse<bool>.Failure("You do not have permission to review this module.");
            }

            if (!string.Equals(dto.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(dto.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResponse<bool>.Failure("Status must be either 'Approved' or 'Rejected'.");
            }

            var applicationIds = (await _unitOfWork.Applications
                    .FindAsync(a => a.ProjectID == module.ProjectId))
                .Select(a => a.ApplicationID)
                .ToHashSet();

            if (applicationIds.Count == 0)
            {
                return ServiceResponse<bool>.Failure("No applications found for this module's project.");
            }

            var progressRecords = (await _unitOfWork.ApplicationModuleProgress
                    .FindAsync(p => p.ProjectModuleId == moduleId))
                .Where(p => applicationIds.Contains(p.ApplicationId))
                .ToList();

            if (progressRecords.Count == 0)
            {
                return ServiceResponse<bool>.Failure("No module progress found to review.");
            }

            if (string.Equals(dto.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                module.Status = ModuleStatus.Approved;

                foreach (var progress in progressRecords)
                {
                    progress.ProgressPercentage = 100;
                    progress.IsCompleted = true;
                    progress.CompletedAt ??= DateTime.UtcNow;
                    progress.UpdatedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(dto.CompanyFeedback))
                    {
                        progress.Note = dto.CompanyFeedback;
                    }
                    _unitOfWork.ApplicationModuleProgress.Update(progress);

                    var application = await _unitOfWork.Applications.GetByIdAsync(progress.ApplicationId);
                    if (application is not null &&
                        application.Status != ApplicationStatus.Completed)
                    {
                        application.Status = ApplicationStatus.InProgress;
                        _unitOfWork.Applications.Update(application);
                    }
                }
            }
            else
            {
                module.Status = ModuleStatus.Rejected;

                foreach (var progress in progressRecords)
                {
                    progress.IsCompleted = false;
                    progress.ProgressPercentage = Math.Min(progress.ProgressPercentage, 99);
                    progress.UpdatedAt = DateTime.UtcNow;
                    progress.Note = dto.CompanyFeedback;
                    _unitOfWork.ApplicationModuleProgress.Update(progress);

                    var application = await _unitOfWork.Applications.GetByIdAsync(progress.ApplicationId);
                    if (application is not null &&
                        application.Status != ApplicationStatus.Completed)
                    {
                        application.Status = ApplicationStatus.InProgress;
                        _unitOfWork.Applications.Update(application);
                    }
                }
            }

            _unitOfWork.ProjectModules.Update(module);
            await _unitOfWork.SaveAsync();

            return ServiceResponse<bool>.Success(true, "Module review submitted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Failure(
                "An error occurred while reviewing the module.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> DeleteModuleAsync(int companyUserId, int moduleId)
    {
        try
        {
            var module = await _unitOfWork.ProjectModules.GetByIdAsync(moduleId);
            if (module is null)
            {
                return ServiceResponse<bool>.Failure("Module not found.");
            }

            // Verify company owns the project
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == companyUserId);
            if (company is null)
            {
                return ServiceResponse<bool>.Failure("Company profile not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(module.ProjectId);
            if (project is null || project.CompanyID != company.CompanyID)
            {
                return ServiceResponse<bool>.Failure("You do not have permission to modify this module.");
            }

            // Check if any progress exists for this module
            var progressRecords = await _unitOfWork.ApplicationModuleProgress
                .FindAsync(p => p.ProjectModuleId == moduleId);

            if (progressRecords.Any())
            {
                return ServiceResponse<bool>.Failure(
                    "Cannot delete module with existing progress records. Module has been started by applicants.");
            }

            _unitOfWork.ProjectModules.Remove(module);
            await _unitOfWork.SaveAsync();

            return ServiceResponse<bool>.Success(true, "Module deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Failure("An error occurred while deleting the module.",
                new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// Checks if all modules are complete and updates application status accordingly.
    /// </summary>
    private async Task CheckAndCompleteApplicationAsync(Application application, Student student)
    {
        // If application is just "Accepted", move it to "InProgress" when first progress is made
        if (application.Status == ApplicationStatus.Accepted)
        {
            application.Status = ApplicationStatus.InProgress;
            _unitOfWork.Applications.Update(application);
            await _unitOfWork.SaveAsync();
        }

        var modules = await _unitOfWork.ProjectModules.FindAsync(m => m.ProjectId == application.ProjectID);
        var progressRecords = await _unitOfWork.ApplicationModuleProgress
            .FindAsync(p => p.ApplicationId == application.ApplicationID);

        var moduleIds = modules.Select(m => m.Id).ToHashSet();
        var completedModuleIds = progressRecords
            .Where(p => p.IsCompleted)
            .Select(p => p.ProjectModuleId)
            .ToHashSet();

        // Check if all modules are complete - notify company but don't auto-complete
        if (moduleIds.Count > 0 && moduleIds.SetEquals(completedModuleIds))
        {
            // All modules are 100% complete - notify the company
            var project = await _unitOfWork.Projects.GetByIdAsync(application.ProjectID);
            if (project is not null)
            {
                var company = await _unitOfWork.Companies.GetByIdAsync(project.CompanyID);
                if (company is not null)
                {
                    var notification = new Notification
                    {
                        UserID = company.UserID,
                        NotificationType = NotificationType.Project,
                        Title = "All Modules Completed",
                        Message = $"{student.FullName} has completed all modules for '{project.ProjectName}'. You can now formally complete the job.",
                        RelatedProjectID = project.ProjectID,
                        RelatedApplicationID = application.ApplicationID,
                        ActionURL = $"/applications/{application.ApplicationID}/progress",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Notifications.AddAsync(notification);
                    await _unitOfWork.SaveAsync();

                    // Send real-time notification
                    var notificationDto = new NotificationDto
                    {
                        Id = notification.NotificationID,
                        Title = notification.Title,
                        Message = notification.Message,
                        NotificationType = notification.NotificationType.ToString(),
                        IsRead = false,
                        CreatedAt = notification.CreatedAt,
                        RelatedProjectId = notification.RelatedProjectID,
                        RelatedApplicationId = notification.RelatedApplicationID,
                        ActionUrl = notification.ActionURL
                    };
                    await _notifier.SendNotificationAsync(company.UserID, notificationDto);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<CompletionSummaryDto>> CompleteJobAsync(int companyUserId, CompleteJobDto dto)
    {
        try
        {
            // 1. Verify the company profile exists
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == companyUserId);
            if (company is null)
            {
                return ServiceResponse<CompletionSummaryDto>.Failure("Company profile not found.");
            }

            // 2. Get the application
            var application = await _unitOfWork.Applications.GetByIdAsync(dto.ApplicationId);
            if (application is null)
            {
                return ServiceResponse<CompletionSummaryDto>.Failure("Application not found.");
            }

            // 3. Verify the company owns the project
            var project = await _unitOfWork.Projects.GetByIdAsync(application.ProjectID);
            if (project is null)
            {
                return ServiceResponse<CompletionSummaryDto>.Failure("Project not found.");
            }

            if (project.CompanyID != company.CompanyID)
            {
                return ServiceResponse<CompletionSummaryDto>.Failure(
                    "You do not have permission to complete this job. Only the project owner can complete it.");
            }

            // 4. Verify the application is currently In Progress or Accepted
            if (application.Status != ApplicationStatus.InProgress && 
                application.Status != ApplicationStatus.Accepted)
            {
                return ServiceResponse<CompletionSummaryDto>.Failure(
                    $"Cannot complete job with status '{application.Status}'. Application must be In Progress or Accepted.");
            }

            // 5. CRUCIAL: Check that ALL ApplicationModuleProgress entries are at 100%
            var modules = await _unitOfWork.ProjectModules.FindAsync(m => m.ProjectId == project.ProjectID);
            var progressRecords = await _unitOfWork.ApplicationModuleProgress
                .FindAsync(p => p.ApplicationId == dto.ApplicationId);

            var moduleIds = modules.Select(m => m.Id).ToHashSet();
            var completedModuleIds = progressRecords
                .Where(p => p.IsCompleted && p.ProgressPercentage == 100)
                .Select(p => p.ProjectModuleId)
                .ToHashSet();

            // Check if all modules exist and are completed
            var incompleteModules = moduleIds.Except(completedModuleIds).ToList();
            if (incompleteModules.Any())
            {
                return ServiceResponse<CompletionSummaryDto>.Failure(
                    "All modules must be completed before closing the job.");
            }

            // 6. Get student info for the summary and notification
            var student = await _unitOfWork.Students.GetByIdAsync(application.StudentID);
            if (student is null)
            {
                return ServiceResponse<CompletionSummaryDto>.Failure("Student not found.");
            }

            // 7. Perform state changes
            // Update Application status to Completed
            application.Status = ApplicationStatus.Completed;
            application.CompletedAt = DateTime.UtcNow;
            application.CompanyFeedbackNote = dto.CompanyFeedbackNote;
            application.FinalDeliverableUrl = dto.FinalDeliverableUrl;
            _unitOfWork.Applications.Update(application);

            // Update Project status to Closed
            project.Status = ProjectStatus.Closed;
            project.EndDate = DateTime.UtcNow;
            _unitOfWork.Projects.Update(project);

            await _unitOfWork.SaveAsync();

            // 8. Send notification to the student
            var notification = new Notification
            {
                UserID = student.UserID,
                NotificationType = NotificationType.Application,
                Title = "Job Completed!",
                Message = $"Your work on '{project.ProjectName}' has been marked as Completed! You can now request payment.",
                RelatedProjectID = project.ProjectID,
                RelatedApplicationID = application.ApplicationID,
                ActionURL = $"/applications/{application.ApplicationID}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            // Send real-time notification
            var notificationDto = new NotificationDto
            {
                Id = notification.NotificationID,
                Title = notification.Title,
                Message = notification.Message,
                NotificationType = notification.NotificationType.ToString(),
                IsRead = false,
                CreatedAt = notification.CreatedAt,
                RelatedProjectId = notification.RelatedProjectID,
                RelatedApplicationId = notification.RelatedApplicationID,
                ActionUrl = notification.ActionURL
            };
            await _notifier.SendNotificationAsync(student.UserID, notificationDto);

            // 9. Generate certificate automatically
            CertificateDto? certificateDto = null;
            var certResult = await _certificateService.GenerateCertificateAsync(application.ApplicationID);
            if (certResult.IsSuccess)
            {
                certificateDto = certResult.Data;
            }

            // 10. Build completion summary
            var startDate = application.ReviewedAt ?? application.AppliedAt;
            var endDate = application.CompletedAt!.Value;
            var duration = endDate - startDate;
            var durationDays = (int)duration.TotalDays;

            // Build human-readable duration text
            var durationText = BuildDurationText(duration);

            // Get total paid (placeholder - would need integration with Payment system)
            decimal totalPaid = application.BidAmount ?? 0;

            var summary = new CompletionSummaryDto
            {
                ApplicationId = application.ApplicationID,
                ProjectId = project.ProjectID,
                ProjectName = project.ProjectName,
                StudentName = student.FullName ?? "Unknown",
                StartDate = startDate,
                EndDate = endDate,
                DurationDays = durationDays,
                DurationText = durationText,
                TotalPaid = totalPaid,
                TotalModulesCompleted = moduleIds.Count,
                CompanyFeedbackNote = dto.CompanyFeedbackNote,
                FinalDeliverableUrl = dto.FinalDeliverableUrl,
                CertificateGenerated = certificateDto is not null,
                CertificateId = certificateDto?.UniqueId
            };

            return ServiceResponse<CompletionSummaryDto>.Success(summary, "Job completed successfully!");
        }
        catch (Exception ex)
        {
            return ServiceResponse<CompletionSummaryDto>.Failure(
                "An error occurred while completing the job.",
                new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// Builds a human-readable duration text from a TimeSpan.
    /// </summary>
    private static string BuildDurationText(TimeSpan duration)
    {
        var parts = new List<string>();

        if (duration.Days >= 30)
        {
            var months = duration.Days / 30;
            var remainingDays = duration.Days % 30;
            parts.Add($"{months} month{(months > 1 ? "s" : "")}");
            if (remainingDays > 0)
            {
                var weeks = remainingDays / 7;
                var days = remainingDays % 7;
                if (weeks > 0) parts.Add($"{weeks} week{(weeks > 1 ? "s" : "")}");
                if (days > 0) parts.Add($"{days} day{(days > 1 ? "s" : "")}");
            }
        }
        else if (duration.Days >= 7)
        {
            var weeks = duration.Days / 7;
            var days = duration.Days % 7;
            parts.Add($"{weeks} week{(weeks > 1 ? "s" : "")}");
            if (days > 0) parts.Add($"{days} day{(days > 1 ? "s" : "")}");
        }
        else if (duration.Days > 0)
        {
            parts.Add($"{duration.Days} day{(duration.Days > 1 ? "s" : "")}");
        }
        else
        {
            parts.Add("Less than a day");
        }

        return string.Join(", ", parts);
    }
}
