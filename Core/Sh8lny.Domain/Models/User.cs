using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sh8lny.Domain.Models
{
    public class User
    {
        // Primary key
        public int UserID { get; set; }

        // Personal information
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // Authentication
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public UserType UserType { get; set; }
        public string? FcmToken { get; set; }

        // Verification
        public bool IsEmailVerified { get; set; }
        public string? VerificationCode { get; set; }
        public DateTime? VerificationCodeExpiry { get; set; }

        // Password Reset
        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }

        // Status and timestamps
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // One-to-one navigation properties
        public Student? Student { get; set; }
        public Company? Company { get; set; }
        public University? University { get; set; }
        public UserSettings? Settings { get; set; }

        // Collections
        public ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new HashSet<ConversationParticipant>();
        public ICollection<Message> SentMessages { get; set; } = new HashSet<Message>();
        public ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();
        public ICollection<ActivityLog> ActivityLogs { get; set; } = new HashSet<ActivityLog>();
        public ICollection<CompletedOpportunity> VerifiedOpportunities { get; set; } = new HashSet<CompletedOpportunity>();
    }

    /// <summary>
    /// User type enumeration
    /// </summary>
    public enum UserType
    {
        Student,
        Company,
        University,
        Admin
    }
}
