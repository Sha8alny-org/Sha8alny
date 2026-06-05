using Microsoft.Extensions.Logging;
using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.TrainingSubmission;

namespace Sh8lny.Service;

/// <summary>
/// Service for managing field training/internship document submissions.
/// Implements a dual-approval workflow requiring both Admin academic approval and Company industry verification.
/// </summary>
public class TrainingSubmissionService : ITrainingSubmissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TrainingSubmissionService> _logger;

    public TrainingSubmissionService(IUnitOfWork unitOfWork, ILogger<TrainingSubmissionService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<TrainingSubmissionResponseDto>> SubmitDocumentsAsync(int studentId, SubmitTrainingDocumentsDto dto)
    {
        try
        {
            // Validate that the application exists and belongs to the student
            var application = await _unitOfWork.Applications.GetByIdAsync(dto.ApplicationID);
            if (application == null)
            {
                return ServiceResponse<TrainingSubmissionResponseDto>.Failure("Application not found.");
            }

            if (application.StudentID != studentId)
            {
                return ServiceResponse<TrainingSubmissionResponseDto>.Failure("You can only submit training documents for your own applications.");
            }

            // Create new training submission
            var submission = new TrainingSubmission
            {
                ApplicationID = dto.ApplicationID,
                StudentID = studentId,
                CertificateUrl = dto.CertificateUrl,
                ReportUrl = dto.ReportUrl,
                PresentationUrl = dto.PresentationUrl,
                CompanyEvaluationUrl = dto.CompanyEvaluationUrl,
                StudentSurveyUrl = dto.StudentSurveyUrl,
                TrainingDays = dto.TrainingDays,
                Status = TrainingSubmissionStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.TrainingSubmissions.AddAsync(submission);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Training submission {SubmissionId} created by student {StudentId}", submission.TrainingSubmissionID, studentId);

            return ServiceResponse<TrainingSubmissionResponseDto>.Success(MapToDto(submission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating training submission for student {StudentId}", studentId);
            return ServiceResponse<TrainingSubmissionResponseDto>.Failure("An error occurred while submitting training documents.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<TrainingSubmissionResponseDto>> AdminReviewAsync(int submissionId, int adminId, AdminReviewTrainingDto dto)
    {
        try
        {
            var submission = await _unitOfWork.TrainingSubmissions.GetByIdAsync(submissionId);
            if (submission == null)
            {
                return ServiceResponse<TrainingSubmissionResponseDto>.Failure("Training submission not found.");
            }

            if (submission.Status == TrainingSubmissionStatus.FullyCompleted)
            {
                return ServiceResponse<TrainingSubmissionResponseDto>.Failure("Cannot review a fully completed submission.");
            }

            if (submission.Status == TrainingSubmissionStatus.Rejected)
            {
                return ServiceResponse<TrainingSubmissionResponseDto>.Failure("Cannot review a rejected submission.");
            }

            // Update admin review fields
            submission.IsAdminApproved = dto.IsApproved;
            submission.AdminNotes = dto.AdminNotes;
            submission.ReviewedByAdminId = adminId;
            submission.AdminReviewedAt = DateTime.UtcNow;
            submission.UpdatedAt = DateTime.UtcNow;

            if (!dto.IsApproved)
            {
                submission.Status = TrainingSubmissionStatus.Rejected;
                submission.RejectionReason = dto.RejectionReason;
            }
            else
            {
                submission.Status = TrainingSubmissionStatus.AdminApproved;
                await CheckAndFinalizeAsync(submission);
            }

            _unitOfWork.TrainingSubmissions.Update(submission);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Training submission {SubmissionId} reviewed by admin {AdminId}. Approved: {IsApproved}", submissionId, adminId, dto.IsApproved);

            return ServiceResponse<TrainingSubmissionResponseDto>.Success(MapToDto(submission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing training submission {SubmissionId}", submissionId);
            return ServiceResponse<TrainingSubmissionResponseDto>.Failure("An error occurred while reviewing the submission.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<TrainingSubmissionResponseDto>> CompanyVerifyAsync(int submissionId, int companyId)
    {
        try
        {
            var submission = await _unitOfWork.TrainingSubmissions.GetByIdAsync(submissionId);
            if (submission == null)
            {
                return ServiceResponse<TrainingSubmissionResponseDto>.Failure("Training submission not found.");
            }

            if (submission.Status == TrainingSubmissionStatus.FullyCompleted)
            {
                return ServiceResponse<TrainingSubmissionResponseDto>.Failure("Cannot verify a fully completed submission.");
            }

            if (submission.Status == TrainingSubmissionStatus.Rejected)
            {
                return ServiceResponse<TrainingSubmissionResponseDto>.Failure("Cannot verify a rejected submission.");
            }

            // Verify that the company owns the project associated with the application
            var application = await _unitOfWork.Applications.GetByIdAsync(submission.ApplicationID);
            if (application == null)
            {
                return ServiceResponse<TrainingSubmissionResponseDto>.Failure("Associated application not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(application.ProjectID);
            if (project == null || project.CompanyID != companyId)
            {
                return ServiceResponse<TrainingSubmissionResponseDto>.Failure("You can only verify training submissions for your own company's projects.");
            }

            // Update company verification fields
            submission.IsCompanyVerified = true;
            submission.CompanyVerifiedAt = DateTime.UtcNow;
            submission.Status = TrainingSubmissionStatus.CompanyVerified;
            submission.UpdatedAt = DateTime.UtcNow;

            await CheckAndFinalizeAsync(submission);

            _unitOfWork.TrainingSubmissions.Update(submission);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Training submission {SubmissionId} verified by company {CompanyId}", submissionId, companyId);

            return ServiceResponse<TrainingSubmissionResponseDto>.Success(MapToDto(submission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying training submission {SubmissionId}", submissionId);
            return ServiceResponse<TrainingSubmissionResponseDto>.Failure("An error occurred while verifying the submission.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<TrainingSubmissionResponseDto>> GetByIdAsync(int submissionId)
    {
        try
        {
            var submission = await _unitOfWork.TrainingSubmissions.GetByIdAsync(submissionId);
            if (submission == null)
            {
                return ServiceResponse<TrainingSubmissionResponseDto>.Failure("Training submission not found.");
            }

            return ServiceResponse<TrainingSubmissionResponseDto>.Success(MapToDto(submission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training submission {SubmissionId}", submissionId);
            return ServiceResponse<TrainingSubmissionResponseDto>.Failure("An error occurred while retrieving the submission.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>> GetByApplicationAsync(int applicationId)
    {
        try
        {
            var submissions = await _unitOfWork.TrainingSubmissions.FindAsync(ts => ts.ApplicationID == applicationId);
            var dtos = submissions.Select(MapToDto).ToList();

            return ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training submissions for application {ApplicationId}", applicationId);
            return ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>.Failure("An error occurred while retrieving submissions.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>> GetByStudentAsync(int studentId)
    {
        try
        {
            var submissions = await _unitOfWork.TrainingSubmissions.FindAsync(ts => ts.StudentID == studentId);
            var dtos = submissions.Select(MapToDto).ToList();

            return ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training submissions for student {StudentId}", studentId);
            return ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>.Failure("An error occurred while retrieving submissions.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>> GetPendingForAdminAsync()
    {
        try
        {
            var submissions = await _unitOfWork.TrainingSubmissions.FindAsync(ts => 
                ts.Status == TrainingSubmissionStatus.Pending && !ts.IsAdminApproved);
            var dtos = submissions.Select(MapToDto).ToList();

            return ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending training submissions for admin");
            return ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>.Failure("An error occurred while retrieving pending submissions.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>> GetPendingForCompanyAsync(int companyId)
    {
        try
        {
            // Get all projects for this company
            var companyProjects = await _unitOfWork.Projects.FindAsync(p => p.CompanyID == companyId);
            var projectIds = companyProjects.Select(p => p.ProjectID).ToList();

            // Get all applications for these projects
            var applications = await _unitOfWork.Applications.FindAsync(a => projectIds.Contains(a.ProjectID));
            var applicationIds = applications.Select(a => a.ApplicationID).ToList();

            // Get pending submissions for these applications
            var submissions = await _unitOfWork.TrainingSubmissions.FindAsync(ts => 
                applicationIds.Contains(ts.ApplicationID) && 
                ts.IsAdminApproved && 
                !ts.IsCompanyVerified);

            var dtos = submissions.Select(MapToDto).ToList();

            return ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending training submissions for company {CompanyId}", companyId);
            return ServiceResponse<IEnumerable<TrainingSubmissionResponseDto>>.Failure("An error occurred while retrieving pending submissions.");
        }
    }

    /// <summary>
    /// Checks if a submission can be fully completed (both admin approved and company verified).
    /// If so, updates the status and increments the student's TotalInternshipDays.
    /// </summary>
    private async Task CheckAndFinalizeAsync(TrainingSubmission submission)
    {
        if (submission.IsAdminApproved && submission.IsCompanyVerified)
        {
            submission.Status = TrainingSubmissionStatus.FullyCompleted;
            submission.CompletedAt = DateTime.UtcNow;

            // Increment student's TotalInternshipDays if TrainingDays is specified
            if (submission.TrainingDays.HasValue && submission.TrainingDays.Value > 0)
            {
                var student = await _unitOfWork.Students.GetByIdAsync(submission.StudentID);
                if (student != null)
                {
                    student.TotalInternshipDays += submission.TrainingDays.Value;
                    student.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Students.Update(student);

                    _logger.LogInformation("Added {TrainingDays} days to student {StudentId}. New total: {TotalDays}", 
                        submission.TrainingDays.Value, student.StudentID, student.TotalInternshipDays);
                }
            }
        }
        else if (submission.IsAdminApproved)
        {
            submission.Status = TrainingSubmissionStatus.AdminApproved;
        }
        else if (submission.IsCompanyVerified)
        {
            submission.Status = TrainingSubmissionStatus.CompanyVerified;
        }
    }

    /// <summary>
    /// Maps a TrainingSubmission entity to a TrainingSubmissionResponseDto.
    /// </summary>
    private TrainingSubmissionResponseDto MapToDto(TrainingSubmission submission)
    {
        return new TrainingSubmissionResponseDto
        {
            TrainingSubmissionID = submission.TrainingSubmissionID,
            ApplicationID = submission.ApplicationID,
            StudentID = submission.StudentID,
            CertificateUrl = submission.CertificateUrl,
            ReportUrl = submission.ReportUrl,
            PresentationUrl = submission.PresentationUrl,
            CompanyEvaluationUrl = submission.CompanyEvaluationUrl,
            StudentSurveyUrl = submission.StudentSurveyUrl,
            Status = submission.Status.ToString(),
            IsAdminApproved = submission.IsAdminApproved,
            IsCompanyVerified = submission.IsCompanyVerified,
            TrainingDays = submission.TrainingDays,
            AdminNotes = submission.AdminNotes,
            RejectionReason = submission.RejectionReason,
            ReviewedByAdminId = submission.ReviewedByAdminId,
            AdminReviewedAt = submission.AdminReviewedAt,
            CompanyVerifiedAt = submission.CompanyVerifiedAt,
            CompletedAt = submission.CompletedAt,
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt
        };
    }
}
