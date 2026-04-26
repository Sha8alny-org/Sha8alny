using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sh8lny.Domain.Models
{
    public class Project
    {
        // Primary key
        public int ProjectID { get; set; }

        // Foreign key
        public int CompanyID { get; set; }

        // Project details
        public required string ProjectName { get; set; }
        public string? ProjectCode { get; set; }
        public required string Description { get; set; }
        public ProjectType? ProjectType { get; set; }

        // Timeline
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime Deadline { get; set; }
        public string? Duration { get; set; }

        // Requirements
        public string? RequiredSkills { get; set; }
        public string? MinAcademicYear { get; set; }
        public int? MaxApplicants { get; set; }

        // Status
        public ProjectStatus Status { get; set; }
        public bool IsVisible { get; set; }

        // Creator info
        public int CreatedBy { get; set; }
        public string? CreatedByName { get; set; }

        // Metrics
        public int ViewCount { get; set; }
        public int ApplicationCount { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;

        // Collections
        public ICollection<ProjectRequiredSkill> ProjectRequiredSkills { get; set; } = new HashSet<ProjectRequiredSkill>();
        public ICollection<Application> Applications { get; set; } = new HashSet<Application>();
        public ICollection<ProjectGroup> ProjectGroups { get; set; } = new HashSet<ProjectGroup>();
        public ICollection<Conversation> Conversations { get; set; } = new HashSet<Conversation>();
        public ICollection<Certificate> Certificates { get; set; } = new HashSet<Certificate>();
        public ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
        public ICollection<CompletedOpportunity> CompletedOpportunities { get; set; } = new HashSet<CompletedOpportunity>();
        public ICollection<ProjectModule> Modules { get; set; } = new HashSet<ProjectModule>();
        public ICollection<SavedOpportunity> SavedOpportunities { get; set; } = new HashSet<SavedOpportunity>();
    }

    /// <summary>
    /// Project type enumeration
    /// </summary>
    public enum ProjectType
    {
        Internship,
        GraduationProject,
        Training,
        PartTime,
        FullTime
    }

    /// <summary>
    /// Project status enumeration
    /// </summary>
    public enum ProjectStatus
    {
        Draft,
        Active,
        Pending,
        Complete,
        Cancelled,
        Closed
    }
}
