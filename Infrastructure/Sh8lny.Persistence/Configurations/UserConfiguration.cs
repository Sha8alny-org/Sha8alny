using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sh8lny.Domain.Models;

namespace Sh8lny.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Table mapping
            builder.ToTable("Users");

            // Primary key
            builder.HasKey(u => u.UserID);

            // Properties
            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.UserType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(u => u.IsEmailVerified)
                .HasDefaultValue(false);

            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            builder.Property(u => u.VerificationCode)
                .HasMaxLength(10);

            builder.Property(u => u.FcmToken)
                .HasMaxLength(512);

            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            builder.Property(u => u.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Indexes
            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IDX_Users_Email");

            builder.HasIndex(u => u.UserType)
                .HasDatabaseName("IDX_Users_UserType");

            // One-to-one relationships
            builder.HasOne(u => u.Student)
                .WithOne(s => s.User)
                .HasForeignKey<Student>(s => s.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.Company)
                .WithOne(c => c.User)
                .HasForeignKey<Company>(c => c.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.Settings)
                .WithOne(s => s.User)
                .HasForeignKey<UserSettings>(s => s.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many relationships
            builder.HasMany(u => u.ConversationParticipants)
                .WithOne(cp => cp.User)
                .HasForeignKey(cp => cp.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.SentMessages)
                .WithOne(m => m.Sender)
                .HasForeignKey(m => m.SenderID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.VerifiedOpportunities)
                .WithOne(co => co.Verifier)
                .HasForeignKey(co => co.VerifiedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // ActivityLogs relationship removed from model
        }
    }
}
