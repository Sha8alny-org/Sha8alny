using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sh8lny.Abstraction.Repositories;
using Sh8lny.Domain.Models;
using Sh8lny.Persistence.Contexts;

namespace Sh8lny.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly Sha8lnyDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed;
        private IGenericRepository<User>? _users;
        private IGenericRepository<Student>? _students;
        private IGenericRepository<Company>? _companies;
        private IGenericRepository<University>? _universities;
        private IGenericRepository<Department>? _departments;
        private IGenericRepository<Skill>? _skills;
        private IGenericRepository<StudentSkill>? _studentSkills;
        private IGenericRepository<Education>? _educations;
        private IGenericRepository<Experience>? _experiences;
        private IGenericRepository<Project>? _projects;
        private IGenericRepository<ProjectRequiredSkill>? _projectRequiredSkills;
        private IGenericRepository<ProjectModule>? _projectModules;
        private IGenericRepository<Application>? _applications;
        private IGenericRepository<ApplicationModuleProgress>? _applicationModuleProgress;
        private IGenericRepository<ProjectGroup>? _projectGroups;
        private IGenericRepository<GroupMember>? _groupMembers;
        private IGenericRepository<Conversation>? _conversations;
        private IGenericRepository<ConversationParticipant>? _conversationParticipants;
        private IGenericRepository<Message>? _messages;
        private IGenericRepository<Certificate>? _certificates;
        private IGenericRepository<Notification>? _notifications;
        private IGenericRepository<ActivityLog>? _activityLogs;
        private IGenericRepository<DashboardMetric>? _dashboardMetrics;
        private IGenericRepository<UserSettings>? _userSettings;
        private IGenericRepository<Payment>? _payments;
        private IGenericRepository<CompletedOpportunity>? _completedOpportunities;
        private IGenericRepository<CompanyReview>? _companyReviews;
        private IGenericRepository<StudentReview>? _studentReviews;
        private IGenericRepository<SavedOpportunity>? _savedOpportunities;
        private IGenericRepository<Transaction>? _transactions;

        public UnitOfWork(Sha8lnyDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public IGenericRepository<User> Users =>
            _users ??= new GenericRepository<User>(_context);

        public IGenericRepository<Student> Students =>
            _students ??= new GenericRepository<Student>(_context);

        public IGenericRepository<Company> Companies =>
            _companies ??= new GenericRepository<Company>(_context);

        public IGenericRepository<University> Universities =>
            _universities ??= new GenericRepository<University>(_context);

        public IGenericRepository<Department> Departments =>
            _departments ??= new GenericRepository<Department>(_context);

        public IGenericRepository<Skill> Skills =>
            _skills ??= new GenericRepository<Skill>(_context);

        public IGenericRepository<StudentSkill> StudentSkills =>
            _studentSkills ??= new GenericRepository<StudentSkill>(_context);

        public IGenericRepository<Education> Educations =>
            _educations ??= new GenericRepository<Education>(_context);

        public IGenericRepository<Experience> Experiences =>
            _experiences ??= new GenericRepository<Experience>(_context);

        public IGenericRepository<Project> Projects =>
            _projects ??= new GenericRepository<Project>(_context);

        public IGenericRepository<ProjectRequiredSkill> ProjectRequiredSkills =>
            _projectRequiredSkills ??= new GenericRepository<ProjectRequiredSkill>(_context);

        public IGenericRepository<ProjectModule> ProjectModules =>
            _projectModules ??= new GenericRepository<ProjectModule>(_context);

        public IGenericRepository<Application> Applications =>
            _applications ??= new GenericRepository<Application>(_context);

        public IGenericRepository<ApplicationModuleProgress> ApplicationModuleProgress =>
            _applicationModuleProgress ??= new GenericRepository<ApplicationModuleProgress>(_context);

        public IGenericRepository<ProjectGroup> ProjectGroups =>
            _projectGroups ??= new GenericRepository<ProjectGroup>(_context);

        public IGenericRepository<GroupMember> GroupMembers =>
            _groupMembers ??= new GenericRepository<GroupMember>(_context);

        public IGenericRepository<Conversation> Conversations =>
            _conversations ??= new GenericRepository<Conversation>(_context);

        public IGenericRepository<ConversationParticipant> ConversationParticipants =>
            _conversationParticipants ??= new GenericRepository<ConversationParticipant>(_context);

        public IGenericRepository<Message> Messages =>
            _messages ??= new GenericRepository<Message>(_context);

        public IGenericRepository<Certificate> Certificates =>
            _certificates ??= new GenericRepository<Certificate>(_context);

        public IGenericRepository<Notification> Notifications =>
            _notifications ??= new GenericRepository<Notification>(_context);

        public IGenericRepository<ActivityLog> ActivityLogs =>
            _activityLogs ??= new GenericRepository<ActivityLog>(_context);

        public IGenericRepository<DashboardMetric> DashboardMetrics =>
            _dashboardMetrics ??= new GenericRepository<DashboardMetric>(_context);

        public IGenericRepository<UserSettings> UserSettings =>
            _userSettings ??= new GenericRepository<UserSettings>(_context);

        public IGenericRepository<Payment> Payments =>
            _payments ??= new GenericRepository<Payment>(_context);

        public IGenericRepository<CompletedOpportunity> CompletedOpportunities =>
            _completedOpportunities ??= new GenericRepository<CompletedOpportunity>(_context);

        public IGenericRepository<CompanyReview> CompanyReviews =>
            _companyReviews ??= new GenericRepository<CompanyReview>(_context);

        public IGenericRepository<StudentReview> StudentReviews =>
            _studentReviews ??= new GenericRepository<StudentReview>(_context);

        public IGenericRepository<SavedOpportunity> SavedOpportunities =>
            _savedOpportunities ??= new GenericRepository<SavedOpportunity>(_context);

        public IGenericRepository<Transaction> Transactions =>
            _transactions ??= new GenericRepository<Transaction>(_context);

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<int> SaveAsync(CancellationToken cancellationToken)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task<Student?> GetStudentWithSkillsAsync(int userId)
        {
            return await _context.Students
                .Include(s => s.StudentSkills)
                    .ThenInclude(ss => ss.Skill)
                .Include(s => s.Educations)
                .Include(s => s.Experiences)
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.UserID == userId);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  // to suppress el garbage collector finalizer (nigga takes alot of resources and we might want a soft delete lol)
        }
    }
}
