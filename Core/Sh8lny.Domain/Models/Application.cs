using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sh8lny.Domain.Models
{
    public class Application
    {
        // Primary key
        public int ApplicationID { get; set; }

        // Foreign keys
        public int ProjectID { get; set; }
        public int StudentID { get; set; }

        // Application details
        public string? CoverLetter { get; set; }
        public required string Resume { get; set; }
        public string? PortfolioURL { get; set; }
        public string? ProposalFileUrl { get; set; }
        public string? StudentCvUrl { get; set; }
        public decimal? BidAmount { get; set; }

        // Status tracking
        public ApplicationStatus Status { get; set; }
        //public TimePreference? TimePreference { get; set; }

        // Review info
        public int? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }

        // Completion info
        public DateTime? CompletedAt { get; set; }
        public string? CompanyFeedbackNote { get; set; }
        public string? FinalDeliverableUrl { get; set; }

        // Payment info
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }

        // Timestamps
        public DateTime AppliedAt { get; set; }
        //public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public Project Project { get; set; } = null!;
        public Student Student { get; set; } = null!;
        //public User? Reviewer { get; set; }
        public CompletedOpportunity? CompletedOpportunity { get; set; }
        public ICollection<ApplicationModuleProgress> ModuleProgress { get; set; } = new HashSet<ApplicationModuleProgress>();
    }

    /// <summary>
    /// Application status enumeration
    /// </summary>
    public enum ApplicationStatus
    {
        Submit,
        Pending,
        UnderReview,
        Accepted,
        InProgress,
        Completed,
        Rejected,
        Withdrawn
    }




    /// <summary>
    /// Time preference enumeration (from Figma mobile UI)
    /// </summary>
    // public enum TimePreference
    //{    AM,PM,Flexible}
}

