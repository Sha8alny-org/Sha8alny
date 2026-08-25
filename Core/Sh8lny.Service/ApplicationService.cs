using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Applications;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Notifications;

namespace Sh8lny.Service;

/// <summary>
/// Service for application operations.
/// </summary>
public class ApplicationService : IApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotifier _notifier;

    public ApplicationService(IUnitOfWork unitOfWork, INotifier notifier)
    {
        _unitOfWork = unitOfWork;
        _notifier = notifier;
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<int>> ApplyForProjectAsync(int studentUserId, CreateApplicationDto dto)
    {
        try
        {
            // 1. Check if student profile exists
            var student = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == studentUserId);
            if (student is null)
            {
                return ServiceResponse<int>.Failure("Student profile not found. Please create your profile first.");
            }

            // 2. Check if project exists
            var project = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId);
            if (project is null)
            {
                return ServiceResponse<int>.Failure("Project not found.");
            }

            // 3. Check if project deadline has passed
            if (project.Deadline < DateTime.UtcNow)
            {
                return ServiceResponse<int>.Failure("The application deadline for this project has passed.");
            }

            // 4. Check if project is visible/active
            if (!project.IsVisible || project.Status != ProjectStatus.Active)
            {
                return ServiceResponse<int>.Failure("This project is no longer accepting applications.");
            }

            // 5. Check if max applicants reached
            if (project.MaxApplicants.HasValue && project.ApplicationCount >= project.MaxApplicants.Value)
            {
                return ServiceResponse<int>.Failure("This project has reached the maximum number of applicants.");
            }

            // 6. Check for duplicate application
            var existingApplication = await _unitOfWork.Applications
                .FindSingleAsync(a => a.ProjectID == dto.ProjectId && a.StudentID == student.StudentID);
            if (existingApplication is not null)
            {
                return ServiceResponse<int>.Failure("You have already applied for this project.");
            }

            // 7. Skill check - compare required skills vs student skills
            var requiredSkills = await _unitOfWork.ProjectRequiredSkills
                .FindAsync(ps => ps.ProjectID == dto.ProjectId && ps.IsRequired);
            var studentSkills = await _unitOfWork.StudentSkills
                .FindAsync(ss => ss.StudentID == student.StudentID);

            var requiredSkillIds = requiredSkills.Select(rs => rs.SkillID).ToHashSet();
            var studentSkillIds = studentSkills.Select(ss => ss.SkillID).ToHashSet();

            var missingSkills = requiredSkillIds.Except(studentSkillIds).ToList();
            if (missingSkills.Count > 0)
            {
                // Get skill names for better error message
                var missingSkillNames = new List<string>();
                foreach (var skillId in missingSkills)
                {
                    var skill = await _unitOfWork.Skills.GetByIdAsync(skillId);
                    if (skill is not null)
                    {
                        missingSkillNames.Add(skill.SkillName);
                    }
                }

                return ServiceResponse<int>.Failure(
                    $"You are missing the following required skills: {string.Join(", ", missingSkillNames)}",
                    missingSkillNames);
            }

            var projectType = project.ProjectType?.ToString();
            decimal resolvedBidAmount;
            if (string.Equals(projectType, "Internship", StringComparison.OrdinalIgnoreCase))
            {
                resolvedBidAmount = 0;
            }
            else
            {
                resolvedBidAmount = dto.BidAmount;
            }

            // Create application
            var application = new Application
            {
                ProjectID = dto.ProjectId,
                StudentID = student.StudentID,
                ProposalFileUrl = dto.ProposalFileUrl,
                StudentCvUrl = dto.StudentCvUrl,
                BidAmount = resolvedBidAmount,
                Resume = dto.StudentCvUrl,
                Status = ApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            };

            await _unitOfWork.Applications.AddAsync(application);

            // Increment project application count
            project.ApplicationCount++;
            _unitOfWork.Projects.Update(project);

            await _unitOfWork.SaveAsync();

            return ServiceResponse<int>.Success(application.ApplicationID, "Application submitted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<int>.Failure("An error occurred while submitting the application.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<IEnumerable<ApplicationResponseDto>>> GetStudentApplicationsAsync(int studentUserId)
    {
        try
        {
            var student = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == studentUserId);
            if (student is null)
            {
                return ServiceResponse<IEnumerable<ApplicationResponseDto>>.Failure("Student profile not found.");
            }

            var applications = await _unitOfWork.Applications
                .FindAsync(a => a.StudentID == student.StudentID);

            var responseDtos = new List<ApplicationResponseDto>();

            foreach (var app in applications)
            {
                var project = await _unitOfWork.Projects.GetByIdAsync(app.ProjectID);

                responseDtos.Add(new ApplicationResponseDto
                {
                    Id = app.ApplicationID,
                    ProjectId = app.ProjectID,
                    ProjectTitle = project?.ProjectName ?? "Unknown Project",
                    Status = app.Status.ToString(),
                    AppliedDate = app.AppliedAt,
                    BidAmount = app.BidAmount ?? 0,
                    StudentCvUrl = app.StudentCvUrl ?? app.Resume
                });
            }

            return ServiceResponse<IEnumerable<ApplicationResponseDto>>.Success(responseDtos);
        }
        catch (Exception ex)
        {
            return ServiceResponse<IEnumerable<ApplicationResponseDto>>.Failure(
                "An error occurred while retrieving applications.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<IEnumerable<ApplicantDto>>> GetProjectApplicationsAsync(int companyUserId, int projectId)
    {
        try
        {
            // Verify company ownership
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == companyUserId);
            if (company is null)
            {
                return ServiceResponse<IEnumerable<ApplicantDto>>.Failure("Company profile not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project is null)
            {
                return ServiceResponse<IEnumerable<ApplicantDto>>.Failure("Project not found.");
            }

            if (project.CompanyID != company.CompanyID)
            {
                return ServiceResponse<IEnumerable<ApplicantDto>>.Failure("You do not have permission to view applications for this project.");
            }

            var applications = await _unitOfWork.Applications.FindAsync(a => a.ProjectID == projectId);
            var applicantDtos = new List<ApplicantDto>();

            foreach (var app in applications)
            {
                var student = await _unitOfWork.Students.GetByIdAsync(app.StudentID);

                applicantDtos.Add(new ApplicantDto
                {
                    ApplicationId = app.ApplicationID,
                    StudentName = student?.FullName ?? "Unknown",
                    StudentTitle = student?.Bio,
                    Proposal = app.ProposalFileUrl,
                    AppliedDate = app.AppliedAt
                });
            }

            return ServiceResponse<IEnumerable<ApplicantDto>>.Success(applicantDtos);
        }
        catch (Exception ex)
        {
            return ServiceResponse<IEnumerable<ApplicantDto>>.Failure(
                "An error occurred while retrieving project applications.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> UpdateApplicationStatusAsync(int companyUserId, int applicationId, UpdateApplicationStatusDto dto)
    {
        try
        {
            // Get application
            var application = await _unitOfWork.Applications.GetByIdAsync(applicationId);
            if (application is null)
            {
                return ServiceResponse<bool>.Failure("Application not found.");
            }

            // Verify company ownership of the project
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == companyUserId);
            if (company is null)
            {
                return ServiceResponse<bool>.Failure("Company profile not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(application.ProjectID);
            if (project is null || project.CompanyID != company.CompanyID)
            {
                return ServiceResponse<bool>.Failure("You do not have permission to update this application.");
            }

            // Parse and validate status
            if (!Enum.TryParse<ApplicationStatus>(dto.Status, ignoreCase: true, out var newStatus))
            {
                return ServiceResponse<bool>.Failure("Invalid application status.");
            }

            // Update application
            application.Status = newStatus;
            application.ReviewedBy = companyUserId;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewNotes = dto.ReviewNotes;

            _unitOfWork.Applications.Update(application);
            await _unitOfWork.SaveAsync();

            return ServiceResponse<bool>.Success(true, $"Application status updated to {newStatus}.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Failure("An error occurred while updating the application.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> ReviewApplicationAsync(int companyUserId, ReviewApplicationDto dto)
    {
        try
        {
            // 1. Get application
            var application = await _unitOfWork.Applications.GetByIdAsync(dto.ApplicationId);
            if (application is null)
            {
                return ServiceResponse<bool>.Failure("Application not found.");
            }

            // 2. Verify company ownership of the project (Crucial Security Check)
            var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == companyUserId);
            if (company is null)
            {
                return ServiceResponse<bool>.Failure("Company profile not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(application.ProjectID);
            if (project is null)
            {
                return ServiceResponse<bool>.Failure("Project not found.");
            }

            if (project.CompanyID != company.CompanyID)
            {
                return ServiceResponse<bool>.Failure("Unauthorized. You do not own this project.");
            }

            // 3. Parse and validate status (only Accepted or Rejected allowed for review)
            if (!Enum.TryParse<ApplicationStatus>(dto.Status, ignoreCase: true, out var newStatus))
            {
                return ServiceResponse<bool>.Failure("Invalid application status.");
            }

            if (newStatus != ApplicationStatus.Accepted && newStatus != ApplicationStatus.Rejected)
            {
                return ServiceResponse<bool>.Failure("Invalid status. Only 'Accepted' or 'Rejected' are allowed for review.");
            }

            // 4. Update application status
            application.Status = newStatus;
            application.ReviewedBy = companyUserId;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewNotes = dto.Note;

            _unitOfWork.Applications.Update(application);

            // 5. Get student to create notification
            var student = await _unitOfWork.Students.GetByIdAsync(application.StudentID);
            if (student is null)
            {
                return ServiceResponse<bool>.Failure("Student not found.");
            }

            // 6. Create notification for the student
            var notificationType = newStatus == ApplicationStatus.Accepted 
                ? NotificationType.Acceptance 
                : NotificationType.Rejection;

            var statusText = newStatus == ApplicationStatus.Accepted ? "Accepted" : "Rejected";
            var notification = new Notification
            {
                UserID = student.UserID,
                NotificationType = notificationType,
                Title = "Application Update",
                Message = $"Your application for '{project.ProjectName}' was {statusText}.",
                RelatedProjectID = project.ProjectID,
                RelatedApplicationID = application.ApplicationID,
                ActionURL = $"/applications/{application.ApplicationID}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);

            // 7. Save all changes
            await _unitOfWork.SaveAsync();

            // 8. Send real-time notification via SignalR
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

            return ServiceResponse<bool>.Success(true, $"Application {statusText.ToLower()} successfully. Student has been notified.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Failure("An error occurred while reviewing the application.",
                new List<string> { ex.Message });
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> WithdrawApplicationAsync(int studentUserId, int applicationId)
    {
        try
        {
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
                return ServiceResponse<bool>.Failure("You do not have permission to withdraw this application.");
            }

            // Can only withdraw pending or under review applications
            if (application.Status != ApplicationStatus.Pending && application.Status != ApplicationStatus.UnderReview)
            {
                return ServiceResponse<bool>.Failure($"Cannot withdraw an application with status '{application.Status}'.");
            }

            application.Status = ApplicationStatus.Withdrawn;
            _unitOfWork.Applications.Update(application);

            // Decrement project application count
            var project = await _unitOfWork.Projects.GetByIdAsync(application.ProjectID);
            if (project is not null && project.ApplicationCount > 0)
            {
                project.ApplicationCount--;
                _unitOfWork.Projects.Update(project);
            }

            await _unitOfWork.SaveAsync();

            return ServiceResponse<bool>.Success(true, "Application withdrawn successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Failure("An error occurred while withdrawing the application.",
                new List<string> { ex.Message });
        }
    }
}
