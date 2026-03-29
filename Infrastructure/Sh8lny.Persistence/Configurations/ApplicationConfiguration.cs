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
    public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
    {
        public void Configure(EntityTypeBuilder<Application> builder)
        {
            // Table mapping
            builder.ToTable("Applications");

            // Primary key
            builder.HasKey(a => a.ApplicationID);

            // Properties
            builder.Property(a => a.CoverLetter)
                .HasMaxLength(2000);

            builder.Property(a => a.Resume)
                .HasMaxLength(500);

            builder.Property(a => a.PortfolioURL)
                .HasMaxLength(500);

            builder.Property(a => a.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(ApplicationStatus.Submit);

            builder.Property(a => a.ReviewNotes)
                .HasMaxLength(1000);

            builder.Property(a => a.AppliedAt)
                .HasDefaultValueSql("GETDATE()");

            // Payment fields
            builder.Property(a => a.IsPaid)
                .HasDefaultValue(false);

            builder.Property(a => a.PaidAt);

            builder.Property(a => a.BidAmount)
                .HasPrecision(18, 2);

            // Completion fields
            builder.Property(a => a.CompletedAt);

            builder.Property(a => a.CompanyFeedbackNote)
                .HasMaxLength(2000);

            builder.Property(a => a.FinalDeliverableUrl)
                .HasMaxLength(500);

            // Unique constraint: one application per student per project
            builder.HasIndex(a => new { a.ProjectID, a.StudentID })
                .IsUnique()
                .HasDatabaseName("UQ_Application_ProjectID_StudentID");

            // Indexes
            builder.HasIndex(a => a.ProjectID)
                .HasDatabaseName("IDX_Applications_ProjectID");

            builder.HasIndex(a => a.StudentID)
                .HasDatabaseName("IDX_Applications_StudentID");

            builder.HasIndex(a => a.Status)
                .HasDatabaseName("IDX_Applications_Status");

            builder.HasIndex(a => a.AppliedAt)
                .HasDatabaseName("IDX_Applications_AppliedAt");

            // Relationships
            builder.HasOne(a => a.Project)
                .WithMany(p => p.Applications)
                .HasForeignKey(a => a.ProjectID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Student)
                .WithMany(s => s.Applications)
                .HasForeignKey(a => a.StudentID)
                .OnDelete(DeleteBehavior.Restrict);

            // Reviewer relationship removed from model
        }
    }
}
