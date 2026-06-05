using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sh8lny.Domain.Models;

namespace Sh8lny.Persistence.Configurations;

public class TrainingSubmissionConfiguration : IEntityTypeConfiguration<TrainingSubmission>
{
    public void Configure(EntityTypeBuilder<TrainingSubmission> builder)
    {
        builder.ToTable("TrainingSubmissions");

        builder.HasKey(ts => ts.TrainingSubmissionID);

        builder.Property(ts => ts.TrainingSubmissionID)
            .ValueGeneratedOnAdd();

        builder.Property(ts => ts.CertificateUrl)
            .HasMaxLength(500);

        builder.Property(ts => ts.ReportUrl)
            .HasMaxLength(500);

        builder.Property(ts => ts.PresentationUrl)
            .HasMaxLength(500);

        builder.Property(ts => ts.CompanyEvaluationUrl)
            .HasMaxLength(500);

        builder.Property(ts => ts.StudentSurveyUrl)
            .HasMaxLength(500);

        builder.Property(ts => ts.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(ts => ts.AdminNotes)
            .HasMaxLength(1000);

        builder.Property(ts => ts.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(ts => ts.SubmittedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(ts => ts.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(ts => ts.Application)
            .WithMany()
            .HasForeignKey(ts => ts.ApplicationID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ts => ts.Student)
            .WithMany()
            .HasForeignKey(ts => ts.StudentID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ts => ts.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(ts => ts.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(ts => ts.ApplicationID);
        builder.HasIndex(ts => ts.StudentID);
        builder.HasIndex(ts => ts.Status);
        builder.HasIndex(ts => ts.SubmittedAt);
    }
}
