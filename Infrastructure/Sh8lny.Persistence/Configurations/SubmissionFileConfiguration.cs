using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sh8lny.Domain.Models;

namespace Sh8lny.Persistence.Configurations;

public class SubmissionFileConfiguration : IEntityTypeConfiguration<SubmissionFile>
{
    public void Configure(EntityTypeBuilder<SubmissionFile> builder)
    {
        builder.ToTable("SubmissionFiles");

        builder.HasKey(sf => sf.SubmissionFileID);

        builder.Property(sf => sf.SubmissionFileID)
            .ValueGeneratedOnAdd();

        builder.Property(sf => sf.FileType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(sf => sf.FileUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(sf => sf.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(SubmissionFileStatus.Pending);

        builder.Property(sf => sf.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(sf => sf.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(sf => sf.TrainingSubmission)
            .WithMany(ts => ts.SubmissionFiles)
            .HasForeignKey(sf => sf.TrainingSubmissionID)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(sf => sf.TrainingSubmissionID)
            .HasDatabaseName("IDX_SubmissionFiles_TrainingSubmissionID");

        builder.HasIndex(sf => sf.FileType)
            .HasDatabaseName("IDX_SubmissionFiles_FileType");

        builder.HasIndex(sf => sf.Status)
            .HasDatabaseName("IDX_SubmissionFiles_Status");
    }
}
