using Microsoft.Extensions.Logging;
using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Training;
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

    /// <inheritdoc />
    public async Task<ServiceResponse<PagedResult<TrainingRecordListItemDto>>> GetFilteredTrainingRecordsAsync(int userId, TrainingRecordFilterDto filter)
    {
        try
        {
            // Admin-only access (Training Unit)
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResponse<PagedResult<TrainingRecordListItemDto>>.Failure("User not found.");
            }

            if (user.UserType != UserType.Admin)
            {
                return ServiceResponse<PagedResult<TrainingRecordListItemDto>>.Failure(
                    "Only administrators can access training records.");
            }

            filter.Normalize();

            var (items, totalCount) = await _unitOfWork.GetFilteredTrainingRecordsAsync(filter);

            var dtos = items.Select(MapToListItemDto).ToList();
            var pagedResult = PagedResult<TrainingRecordListItemDto>.Create(
                dtos, filter.PageNumber, filter.PageSize, totalCount);

            _logger.LogInformation(
                "Retrieved {Count} training records (page {PageNumber} of {TotalPages}) for admin {UserId}",
                dtos.Count, filter.PageNumber, pagedResult.TotalPages, userId);

            return ServiceResponse<PagedResult<TrainingRecordListItemDto>>.Success(
                pagedResult, "Training records retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filtered training records for admin {UserId}", userId);
            return ServiceResponse<PagedResult<TrainingRecordListItemDto>>.Failure(
                "An error occurred while retrieving training records.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<TrainingSubmissionDetailDto>> SubmitDeliverablesAsync(int studentUserId, SubmitTrainingDeliverablesDto dto)
    {
        try
        {
            // 1. Resolve student profile
            var student = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == studentUserId);
            if (student is null)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("Student profile not found. Please create your profile first.");
            }

            // 2. Validate application exists and belongs to the student
            var application = await _unitOfWork.Applications.GetByIdAsync(dto.ApplicationId);
            if (application is null)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("Application not found.");
            }

            if (application.StudentID != student.StudentID)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("You can only submit training deliverables for your own applications.");
            }

            // 3. Application must be in an eligible state (accepted or in training)
            if (application.Status != ApplicationStatus.Accepted && application.Status != ApplicationStatus.InProgress)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure(
                    "Training deliverables can only be submitted for accepted or in-progress applications.");
            }

            // 4. Prevent duplicate submissions for the same application
            var existingSubmission = await _unitOfWork.TrainingSubmissions
                .FindSingleAsync(ts => ts.ApplicationID == dto.ApplicationId);
            if (existingSubmission is not null)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure(
                    "Training deliverables have already been submitted for this application. Use the re-upload endpoint to update rejected files.");
            }

            // 5. Create the submission (legacy URL columns kept in sync)
            var submission = new TrainingSubmission
            {
                ApplicationID = dto.ApplicationId,
                StudentID = student.StudentID,
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

            // 6. Create the per-file deliverable records
            var deliverables = new List<SubmissionFile>
            {
                new() { TrainingSubmissionID = submission.TrainingSubmissionID, FileType = SubmissionFileType.Certificate, FileUrl = dto.CertificateUrl, Status = SubmissionFileStatus.Pending, CreatedAt = DateTime.UtcNow },
                new() { TrainingSubmissionID = submission.TrainingSubmissionID, FileType = SubmissionFileType.Report, FileUrl = dto.ReportUrl, Status = SubmissionFileStatus.Pending, CreatedAt = DateTime.UtcNow },
                new() { TrainingSubmissionID = submission.TrainingSubmissionID, FileType = SubmissionFileType.Presentation, FileUrl = dto.PresentationUrl, Status = SubmissionFileStatus.Pending, CreatedAt = DateTime.UtcNow },
                new() { TrainingSubmissionID = submission.TrainingSubmissionID, FileType = SubmissionFileType.CompanyEvaluation, FileUrl = dto.CompanyEvaluationUrl, Status = SubmissionFileStatus.Pending, CreatedAt = DateTime.UtcNow },
                new() { TrainingSubmissionID = submission.TrainingSubmissionID, FileType = SubmissionFileType.StudentSurvey, FileUrl = dto.StudentSurveyUrl, Status = SubmissionFileStatus.Pending, CreatedAt = DateTime.UtcNow }
            };

            await _unitOfWork.SubmissionFiles.AddRangeAsync(deliverables);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
                "Training deliverables submitted for application {ApplicationId} by student {StudentId} (submission {SubmissionId})",
                dto.ApplicationId, student.StudentID, submission.TrainingSubmissionID);

            var files = await GetSubmissionFilesAsync(submission.TrainingSubmissionID);
            var project = await _unitOfWork.Projects.GetByIdAsync(application.ProjectID);
            return ServiceResponse<TrainingSubmissionDetailDto>.Success(
                MapToDetailDto(submission, files, student.FullName, project),
                "Training deliverables submitted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting training deliverables for application {ApplicationId}", dto.ApplicationId);
            return ServiceResponse<TrainingSubmissionDetailDto>.Failure("An error occurred while submitting the training deliverables.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<TrainingSubmissionDetailDto>> ReviewDeliverablesAsync(int submissionId, int adminUserId, ReviewTrainingDeliverablesDto dto)
    {
        try
        {
            // 1. Admin-only role check
            var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
            if (admin is null)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("User not found.");
            }

            if (admin.UserType != UserType.Admin)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("Only administrators can review training deliverables.");
            }

            // 2. Load submission
            var submission = await _unitOfWork.TrainingSubmissions.GetByIdAsync(submissionId);
            if (submission is null)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("Training submission not found.");
            }

            if (submission.Status == TrainingSubmissionStatus.FullyCompleted)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("Cannot review a fully completed submission.");
            }

            // 3. Load per-file records
            var files = await GetSubmissionFilesAsync(submissionId);
            if (files.Count == 0)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("No deliverable files found for this submission.");
            }

            // 4. Apply per-file decisions
            foreach (var decision in dto.Decisions)
            {
                if (!TryParseDeliverableType(decision.DeliverableType, out var fileType))
                {
                    return ServiceResponse<TrainingSubmissionDetailDto>.Failure(
                        $"Invalid deliverable type '{decision.DeliverableType}'. Valid values: Certificate, Report, Presentation, CompanyEvaluation, StudentSurvey.");
                }

                var file = files.FirstOrDefault(f => f.FileType == fileType);
                if (file is null)
                {
                    return ServiceResponse<TrainingSubmissionDetailDto>.Failure(
                        $"No '{decision.DeliverableType}' deliverable found for this submission.");
                }

                var statusText = decision.Status?.Trim();

                if (string.Equals(statusText, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    file.Status = SubmissionFileStatus.Approved;
                    file.RejectionReason = null;
                }
                else if (string.Equals(statusText, "Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(decision.RejectionReason))
                    {
                        return ServiceResponse<TrainingSubmissionDetailDto>.Failure(
                            $"A rejection reason is required when rejecting the '{decision.DeliverableType}' deliverable.");
                    }

                    file.Status = SubmissionFileStatus.Rejected;
                    file.RejectionReason = decision.RejectionReason.Trim();
                }
                else
                {
                    return ServiceResponse<TrainingSubmissionDetailDto>.Failure(
                        $"Invalid review status '{decision.Status}'. Use 'Approved' or 'Rejected'.");
                }

                file.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.SubmissionFiles.Update(file);
            }

            // 5. Evaluate the overall submission status
            submission.AdminNotes = dto.AdminNotes;
            submission.ReviewedByAdminId = adminUserId;
            submission.AdminReviewedAt = DateTime.UtcNow;
            submission.UpdatedAt = DateTime.UtcNow;

            if (files.All(f => f.Status == SubmissionFileStatus.Approved))
            {
                submission.IsAdminApproved = true;
                submission.RejectionReason = null;
                submission.Status = TrainingSubmissionStatus.AdminApproved;
                await CheckAndFinalizeAsync(submission);
            }
            else
            {
                submission.IsAdminApproved = false;
                submission.Status = TrainingSubmissionStatus.Rejected;

                var reasons = files
                    .Where(f => f.Status == SubmissionFileStatus.Rejected && !string.IsNullOrWhiteSpace(f.RejectionReason))
                    .Select(f => $"{f.FileType}: {f.RejectionReason}");
                var joinedReasons = string.Join(" | ", reasons);
                submission.RejectionReason = joinedReasons.Length > 1000 ? joinedReasons[..1000] : joinedReasons;
            }

            _unitOfWork.TrainingSubmissions.Update(submission);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
                "Training deliverables for submission {SubmissionId} reviewed by admin {AdminId}. Overall status: {Status}",
                submissionId, adminUserId, submission.Status);

            var student = await _unitOfWork.Students.GetByIdAsync(submission.StudentID);
            var reviewedProject = await GetProjectForSubmissionAsync(submission);
            return ServiceResponse<TrainingSubmissionDetailDto>.Success(
                MapToDetailDto(submission, files, student?.FullName, reviewedProject),
                "Training deliverables reviewed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing training deliverables for submission {SubmissionId}", submissionId);
            return ServiceResponse<TrainingSubmissionDetailDto>.Failure("An error occurred while reviewing the training deliverables.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<TrainingSubmissionDetailDto>> ReuploadDeliverableAsync(int studentUserId, int submissionId, ReuploadSingleDeliverableDto dto)
    {
        try
        {
            // 1. Resolve student profile
            var student = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == studentUserId);
            if (student is null)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("Student profile not found.");
            }

            // 2. Load submission and validate ownership
            var submission = await _unitOfWork.TrainingSubmissions.GetByIdAsync(submissionId);
            if (submission is null)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("Training submission not found.");
            }

            if (submission.StudentID != student.StudentID)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("You can only re-upload deliverables for your own submissions.");
            }

            // 3. Parse and validate the deliverable type
            if (!TryParseDeliverableType(dto.DeliverableType, out var fileType))
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure(
                    $"Invalid deliverable type '{dto.DeliverableType}'. Valid values: Certificate, Report, Presentation, CompanyEvaluation, StudentSurvey.");
            }

            // 4. Find the target file
            var files = await GetSubmissionFilesAsync(submissionId);
            var file = files.FirstOrDefault(f => f.FileType == fileType);
            if (file is null)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure(
                    $"No '{dto.DeliverableType}' deliverable found for this submission.");
            }

            // 5. Only rejected deliverables can be re-uploaded
            if (file.Status != SubmissionFileStatus.Rejected)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure(
                    $"Only rejected deliverables can be re-uploaded. The '{dto.DeliverableType}' deliverable is currently '{file.Status}'.");
            }

            // 6. Update only this file — approved files are untouched
            file.FileUrl = dto.NewUrl;
            file.Status = SubmissionFileStatus.Pending;
            file.RejectionReason = null;
            file.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.SubmissionFiles.Update(file);

            // Keep the legacy URL column in sync
            ApplyLegacyUrl(submission, fileType, dto.NewUrl);

            // 7. Return the overall submission to pending admin review
            if (submission.Status == TrainingSubmissionStatus.Rejected)
            {
                submission.Status = TrainingSubmissionStatus.Pending;
            }

            submission.IsAdminApproved = false;
            submission.RejectionReason = null;
            submission.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.TrainingSubmissions.Update(submission);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
                "Deliverable {FileType} of submission {SubmissionId} re-uploaded by student {StudentId}",
                fileType, submissionId, student.StudentID);

            var reuploadProject = await GetProjectForSubmissionAsync(submission);
            return ServiceResponse<TrainingSubmissionDetailDto>.Success(
                MapToDetailDto(submission, files, student.FullName, reuploadProject),
                $"'{fileType}' deliverable re-uploaded successfully. The submission is awaiting admin review again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-uploading deliverable for submission {SubmissionId}", submissionId);
            return ServiceResponse<TrainingSubmissionDetailDto>.Failure("An error occurred while re-uploading the deliverable.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<TrainingSubmissionDetailDto>> GetSubmissionDetailAsync(int submissionId)
    {
        try
        {
            var submission = await _unitOfWork.TrainingSubmissions.GetByIdAsync(submissionId);
            if (submission is null)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("Training submission not found.");
            }

            var files = await GetSubmissionFilesAsync(submissionId);
            var student = await _unitOfWork.Students.GetByIdAsync(submission.StudentID);
            var project = await GetProjectForSubmissionAsync(submission);

            return ServiceResponse<TrainingSubmissionDetailDto>.Success(
                MapToDetailDto(submission, files, student?.FullName, project));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training submission detail {SubmissionId}", submissionId);
            return ServiceResponse<TrainingSubmissionDetailDto>.Failure("An error occurred while retrieving the submission detail.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<TrainingSubmissionDetailDto>> OverrideDurationAsync(int adminUserId, int submissionId, OverrideTrainingDurationDto dto)
    {
        try
        {
            // 1. Admin-only role check
            var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
            if (admin is null)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("User not found.");
            }

            if (admin.UserType != UserType.Admin)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("Only administrators can override the training duration.");
            }

            // 2. Load submission and validate it is still open for updates
            var submission = await _unitOfWork.TrainingSubmissions.GetByIdAsync(submissionId);
            if (submission is null)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure("Training submission not found.");
            }

            if (submission.Status == TrainingSubmissionStatus.FullyCompleted)
            {
                return ServiceResponse<TrainingSubmissionDetailDto>.Failure(
                    "Cannot override the duration of a fully completed submission.");
            }

            // 3. Apply the override
            submission.ApprovedDuration = dto.ApprovedDuration;

            if (!string.IsNullOrWhiteSpace(dto.AdminNotes))
            {
                submission.AdminNotes = string.IsNullOrWhiteSpace(submission.AdminNotes)
                    ? dto.AdminNotes.Trim()
                    : $"{submission.AdminNotes} | {dto.AdminNotes.Trim()}";
            }

            submission.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.TrainingSubmissions.Update(submission);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
                "Training duration for submission {SubmissionId} overridden to {ApprovedDuration} days by admin {AdminId}",
                submissionId, dto.ApprovedDuration, adminUserId);

            var files = await GetSubmissionFilesAsync(submissionId);
            var student = await _unitOfWork.Students.GetByIdAsync(submission.StudentID);
            var project = await GetProjectForSubmissionAsync(submission);

            return ServiceResponse<TrainingSubmissionDetailDto>.Success(
                MapToDetailDto(submission, files, student?.FullName, project),
                $"Approved duration set to {dto.ApprovedDuration} days.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error overriding training duration for submission {SubmissionId}", submissionId);
            return ServiceResponse<TrainingSubmissionDetailDto>.Failure("An error occurred while overriding the training duration.");
        }
    }

    /// <summary>
    /// Checks if a submission can be fully completed (both admin approved and company verified).
    /// If so, updates the status, credits the effective training days to the student's
    /// TotalInternshipDays balance, and reflects completion on the application/project.
    /// </summary>
    private async Task CheckAndFinalizeAsync(TrainingSubmission submission)
    {
        if (submission.IsAdminApproved && submission.IsCompanyVerified)
        {
            // Guard against double-crediting (e.g. a second admin review after completion)
            if (submission.Status == TrainingSubmissionStatus.FullyCompleted)
            {
                return;
            }

            submission.Status = TrainingSubmissionStatus.FullyCompleted;
            submission.CompletedAt = DateTime.UtcNow;

            // Effective credited days priority:
            // 1. Admin-approved override (ApprovedDuration)
            // 2. Declared training days (TrainingDays)
            // 3. Equivalent days calculated from the project listing (hours at a 6-hour training day)
            int? effectiveDays = submission.ApprovedDuration ?? submission.TrainingDays;
            if (!effectiveDays.HasValue)
            {
                var project = await GetProjectForSubmissionAsync(submission);
                effectiveDays = TryGetProjectDurationDays(project);
            }

            if (effectiveDays.HasValue && effectiveDays.Value > 0)
            {
                submission.TrainingDays = effectiveDays.Value;

                var student = await _unitOfWork.Students.GetByIdAsync(submission.StudentID);
                if (student != null)
                {
                    student.TotalInternshipDays += effectiveDays.Value;
                    student.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Students.Update(student);

                    _logger.LogInformation("Added {TrainingDays} days to student {StudentId}. New total: {TotalDays}",
                        effectiveDays.Value, student.StudentID, student.TotalInternshipDays);
                }
            }

            // Reflect completion on the application (and the project when no other
            // active applications remain)
            await MarkApplicationCompletedAsync(submission);
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
    /// Marks the application of a fully completed training submission as Completed,
    /// and completes the project when no other non-terminal applications remain.
    /// </summary>
    private async Task MarkApplicationCompletedAsync(TrainingSubmission submission)
    {
        var application = await _unitOfWork.Applications.GetByIdAsync(submission.ApplicationID);
        if (application == null)
        {
            return;
        }

        if (application.Status != ApplicationStatus.Completed)
        {
            application.Status = ApplicationStatus.Completed;
            application.CompletedAt = DateTime.UtcNow;
            _unitOfWork.Applications.Update(application);
        }

        var otherActiveApplications = await _unitOfWork.Applications.FindAsync(a =>
            a.ProjectID == application.ProjectID &&
            a.ApplicationID != application.ApplicationID &&
            a.Status != ApplicationStatus.Completed &&
            a.Status != ApplicationStatus.Rejected &&
            a.Status != ApplicationStatus.Withdrawn);

        if (!otherActiveApplications.Any())
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(application.ProjectID);
            if (project != null && project.Status != ProjectStatus.Complete)
            {
                project.Status = ProjectStatus.Complete;
                project.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Projects.Update(project);
            }
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

    /// <summary>
    /// Maps a TrainingSubmission (with loaded Student, Department, University, Application, Project, Company)
    /// to a TrainingRecordListItemDto.
    /// </summary>
    private static TrainingRecordListItemDto MapToListItemDto(TrainingSubmission submission)
    {
        var student = submission.Student;
        var project = submission.Application?.Project;
        var calculatedDays = TryGetProjectDurationDays(project);

        return new TrainingRecordListItemDto
        {
            // Student details
            StudentId = student?.StudentID ?? 0,
            StudentName = student?.FullName ?? "Unknown",
            AcademicYear = student?.AcademicYear?.ToString(),
            DepartmentName = student?.Department?.DepartmentName,
            UniversityName = student?.University?.UniversityName,
            Gpa = student?.Gpa,

            // Training details
            ProjectId = project?.ProjectID ?? 0,
            ProjectTitle = project?.ProjectName,
            CompanyId = project?.CompanyID ?? 0,
            CompanyName = project?.Company?.CompanyName,
            ProjectType = project?.ProjectType?.ToString(),
            StartDate = project?.StartDate,
            EndDate = project?.EndDate,
            Duration = project?.Duration,
            DurationType = project?.DurationType?.ToString(),

            // Submission & status info
            SubmissionStatus = submission.Status.ToString(),
            TrainingDays = submission.TrainingDays,
            ApprovedDuration = submission.ApprovedDuration,
            CalculatedDays = calculatedDays,
            CreditedDays = submission.ApprovedDuration ?? submission.TrainingDays ?? calculatedDays
        };
    }

    /// <summary>
    /// The deliverable types supported by the per-file review workflow.
    /// </summary>
    private static readonly HashSet<SubmissionFileType> DeliverableTypes = new()
    {
        SubmissionFileType.Certificate,
        SubmissionFileType.Report,
        SubmissionFileType.Presentation,
        SubmissionFileType.CompanyEvaluation,
        SubmissionFileType.StudentSurvey
    };

    /// <summary>
    /// Attempts to parse a deliverable type string (e.g. "Certificate") into a
    /// <see cref="SubmissionFileType"/> used by the per-file review workflow.
    /// </summary>
    private static bool TryParseDeliverableType(string? deliverableType, out SubmissionFileType fileType)
    {
        fileType = SubmissionFileType.Other;
        return Enum.TryParse(deliverableType, ignoreCase: true, out fileType)
            && DeliverableTypes.Contains(fileType);
    }

    /// <summary>
    /// Calculates the equivalent training days for a duration value.
    /// Hours are converted at the 6-hour standard training day (ceiling);
    /// days are used as-is.
    /// </summary>
    /// <param name="durationValue">The duration value from the project listing.</param>
    /// <param name="type">The duration unit (Days or Hours).</param>
    /// <returns>The equivalent number of training days.</returns>
    private static int CalculateEquivalentDays(int durationValue, DurationType type)
    {
        return type == DurationType.Hours
            ? (int)Math.Ceiling(durationValue / 6.0)
            : durationValue;
    }

    /// <summary>
    /// Derives the equivalent training days from the project listing, using the first
    /// number found in the free-text Duration field and the project's DurationType
    /// (e.g. "2 hours daily for 15 days" with DurationType.Hours → parses "2" → 1 day).
    /// Returns null when the project or its duration cannot be parsed.
    /// </summary>
    private static int? TryGetProjectDurationDays(Project? project)
    {
        if (project is null)
        {
            return null;
        }

        var durationValue = ParseLeadingDurationValue(project.Duration);
        if (!durationValue.HasValue)
        {
            return null;
        }

        return CalculateEquivalentDays(durationValue.Value, project.DurationType ?? DurationType.Days);
    }

    /// <summary>
    /// Extracts the first integer from a free-text duration string (e.g. "30 hours" → 30).
    /// </summary>
    private static int? ParseLeadingDurationValue(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(duration, @"\d+");
        return match.Success && int.TryParse(match.Value, out var value) ? value : null;
    }

    /// <summary>
    /// Gets the project associated with a training submission (via its application).
    /// </summary>
    private async Task<Project?> GetProjectForSubmissionAsync(TrainingSubmission submission)
    {
        var application = await _unitOfWork.Applications.GetByIdAsync(submission.ApplicationID);
        if (application is null)
        {
            return null;
        }

        return await _unitOfWork.Projects.GetByIdAsync(application.ProjectID);
    }

    /// <summary>
    /// Gets the per-file deliverable records for a submission.
    /// </summary>
    private async Task<List<SubmissionFile>> GetSubmissionFilesAsync(int submissionId)
    {
        var files = await _unitOfWork.SubmissionFiles
            .FindAsync(sf => sf.TrainingSubmissionID == submissionId);
        return files.ToList();
    }

    /// <summary>
    /// Keeps the legacy URL column on TrainingSubmission in sync with a re-uploaded file.
    /// </summary>
    private static void ApplyLegacyUrl(TrainingSubmission submission, SubmissionFileType fileType, string url)
    {
        switch (fileType)
        {
            case SubmissionFileType.Certificate:
                submission.CertificateUrl = url;
                break;
            case SubmissionFileType.Report:
                submission.ReportUrl = url;
                break;
            case SubmissionFileType.Presentation:
                submission.PresentationUrl = url;
                break;
            case SubmissionFileType.CompanyEvaluation:
                submission.CompanyEvaluationUrl = url;
                break;
            case SubmissionFileType.StudentSurvey:
                submission.StudentSurveyUrl = url;
                break;
        }
    }

    /// <summary>
    /// Maps a TrainingSubmission with its per-file records to a TrainingSubmissionDetailDto.
    /// </summary>
    private static TrainingSubmissionDetailDto MapToDetailDto(
        TrainingSubmission submission, List<SubmissionFile> files, string? studentName = null, Project? project = null)
    {
        var calculatedDays = TryGetProjectDurationDays(project);

        return new TrainingSubmissionDetailDto
        {
            TrainingSubmissionID = submission.TrainingSubmissionID,
            ApplicationID = submission.ApplicationID,
            StudentID = submission.StudentID,
            StudentName = studentName,
            CertificateUrl = submission.CertificateUrl,
            ReportUrl = submission.ReportUrl,
            PresentationUrl = submission.PresentationUrl,
            CompanyEvaluationUrl = submission.CompanyEvaluationUrl,
            StudentSurveyUrl = submission.StudentSurveyUrl,
            Status = submission.Status.ToString(),
            IsAdminApproved = submission.IsAdminApproved,
            IsCompanyVerified = submission.IsCompanyVerified,
            TrainingDays = submission.TrainingDays,
            ApprovedDuration = submission.ApprovedDuration,
            IsExternalTraining = submission.IsExternalTraining,
            DurationType = project?.DurationType?.ToString(),
            CalculatedDays = calculatedDays,
            CreditedDays = submission.ApprovedDuration ?? submission.TrainingDays ?? calculatedDays,
            AdminNotes = submission.AdminNotes,
            RejectionReason = submission.RejectionReason,
            ReviewedByAdminId = submission.ReviewedByAdminId,
            AdminReviewedAt = submission.AdminReviewedAt,
            CompanyVerifiedAt = submission.CompanyVerifiedAt,
            CompletedAt = submission.CompletedAt,
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt,
            Files = files.Select(f => new TrainingDeliverableFileDto
            {
                FileType = f.FileType.ToString(),
                FileUrl = f.FileUrl,
                Status = f.Status.ToString(),
                RejectionReason = f.RejectionReason,
                UpdatedAt = f.UpdatedAt
            }).ToList()
        };
    }
}
