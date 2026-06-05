using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sh8lny.Domain.Models;

namespace Sh8lny.Persistence.Contexts
{
    public class Sha8lnyDbContext : DbContext
    {
        public Sha8lnyDbContext(DbContextOptions<Sha8lnyDbContext> options) : base(options)
        {

        }
        override protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(Sha8lnyDbContext).Assembly);
        }
        // Core Entities
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<University> Universities { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;

        // Skills
        public DbSet<Skill> Skills { get; set; } = null!;
        public DbSet<StudentSkill> StudentSkills { get; set; } = null!;

        // Projects & Applications
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectRequiredSkill> ProjectRequiredSkills { get; set; } = null!;
        public DbSet<ProjectModule> ProjectModules { get; set; } = null!;
        public DbSet<Application> Applications { get; set; } = null!;
        public DbSet<ApplicationModuleProgress> ApplicationModuleProgress { get; set; } = null!;

        // Teams & Collaboration
        public DbSet<ProjectGroup> ProjectGroups { get; set; } = null!;
        public DbSet<GroupMember> GroupMembers { get; set; } = null!;

        // Messaging
        public DbSet<Conversation> Conversations { get; set; } = null!;
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        // Analytics & Achievements
        public DbSet<Certificate> Certificates { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;
        public DbSet<DashboardMetric> DashboardMetrics { get; set; } = null!;

        // Settings
        public DbSet<UserSettings> UserSettings { get; set; } = null!;

        // Payments & Completed Opportunities
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<CompletedOpportunity> CompletedOpportunities { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;

        // Reviews
        public DbSet<CompanyReview> CompanyReviews { get; set; } = null!;
        public DbSet<StudentReview> StudentReviews { get; set; } = null!;

        // App Configuration
        public DbSet<AppConfig> AppConfigs { get; set; } = null!;

        // Field Training
        public DbSet<TrainingSubmission> TrainingSubmissions { get; set; } = null!;

        // Announcements
        public DbSet<Announcement> Announcements { get; set; } = null!;
    }
}
