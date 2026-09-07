using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Training;

namespace Sh8lny.Abstraction.Repositories
{

    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<User> Users { get; }
        IGenericRepository<Student> Students { get; }
        IGenericRepository<Company> Companies { get; }
        IGenericRepository<University> Universities { get; }
        IGenericRepository<Department> Departments { get; }

        IGenericRepository<Skill> Skills { get; }
        IGenericRepository<StudentSkill> StudentSkills { get; }
        IGenericRepository<Education> Educations { get; }
        IGenericRepository<Experience> Experiences { get; }

        IGenericRepository<Project> Projects { get; }
        IGenericRepository<ProjectRequiredSkill> ProjectRequiredSkills { get; }
        IGenericRepository<ProjectModule> ProjectModules { get; }
        IGenericRepository<Application> Applications { get; }
        IGenericRepository<ApplicationModuleProgress> ApplicationModuleProgress { get; }

        IGenericRepository<ProjectGroup> ProjectGroups { get; }
        IGenericRepository<GroupMember> GroupMembers { get; }

        IGenericRepository<Conversation> Conversations { get; }
        IGenericRepository<ConversationParticipant> ConversationParticipants { get; }
        IGenericRepository<Message> Messages { get; }

        IGenericRepository<Certificate> Certificates { get; }
        IGenericRepository<Notification> Notifications { get; }
        IGenericRepository<ActivityLog> ActivityLogs { get; }
        IGenericRepository<DashboardMetric> DashboardMetrics { get; }

        IGenericRepository<UserSettings> UserSettings { get; }

        IGenericRepository<Payment> Payments { get; }
        IGenericRepository<CompletedOpportunity> CompletedOpportunities { get; }

        IGenericRepository<CompanyReview> CompanyReviews { get; }
        IGenericRepository<StudentReview> StudentReviews { get; }

        IGenericRepository<SavedOpportunity> SavedOpportunities { get; }
        IGenericRepository<Transaction> Transactions { get; }
        IGenericRepository<AppConfig> AppConfigs { get; }

        // Field Training
        IGenericRepository<TrainingSubmission> TrainingSubmissions { get; }
        IGenericRepository<SubmissionFile> SubmissionFiles { get; }

        // Announcements
        IGenericRepository<Announcement> Announcements { get; }

        Task<int> SaveAsync();
        Task<int> SaveAsync(CancellationToken cancellationToken);
        
        
       // for when we add payment to the project (IF WE ADD IT lol)        
       
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        
        Task<Student?> GetStudentWithSkillsAsync(int userId);
        Task<IEnumerable<SavedOpportunity>> GetSavedOpportunitiesWithProjectAsync(int studentId);
        Task<List<Application>> GetApplicationsWithStudentDetailsAsync(int projectId);

        /// <summary>
        /// Gets a filtered, paged page of training/internship records
        /// (TrainingSubmissions with student, department, university, project, and company details).
        /// </summary>
        /// <param name="filter">The filter criteria and pagination parameters.</param>
        /// <returns>The page of training submissions plus the total matching count.</returns>
        Task<(List<TrainingSubmission> Items, int TotalCount)> GetFilteredTrainingRecordsAsync(TrainingRecordFilterDto filter);
    }
}
