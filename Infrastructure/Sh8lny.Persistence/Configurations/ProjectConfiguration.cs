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
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            // Table mapping
            builder.ToTable("Projects");

            // Primary key
            builder.HasKey(p => p.ProjectID);

            // Properties
            builder.Property(p => p.ProjectName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.ProjectCode)
                .HasMaxLength(50);

            builder.HasIndex(p => p.ProjectCode)
                .IsUnique()
                .HasDatabaseName("UQ_Projects_ProjectCode");

            builder.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(p => p.ProjectType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(p => p.RequiredSkills)
                .HasMaxLength(1000);

            // GPA requirement
            builder.Property(p => p.IsGpaRequired)
                .HasDefaultValue(false);

            builder.Property(p => p.MinimumGpa)
                .HasColumnType("decimal(3,2)");

            // Duration unit
            builder.Property(p => p.DurationType)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(p => p.CreatedByName)
                .HasMaxLength(200);

            builder.Property(p => p.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(ProjectStatus.Draft);

            builder.Property(p => p.IsVisible)
                .HasDefaultValue(true);

            builder.Property(p => p.ViewCount)
                .HasDefaultValue(0);

            builder.Property(p => p.ApplicationCount)
                .HasDefaultValue(0);

            builder.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            builder.Property(p => p.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Indexes
            builder.HasIndex(p => p.CompanyID)
                .HasDatabaseName("IDX_Projects_CompanyID");

            builder.HasIndex(p => p.Status)
                .HasDatabaseName("IDX_Projects_Status");

            builder.HasIndex(p => p.Deadline)
                .HasDatabaseName("IDX_Projects_Deadline");

            builder.HasIndex(p => p.ProjectType)
                .HasDatabaseName("IDX_Projects_ProjectType");

            builder.HasIndex(p => p.IsVisible)
                .HasDatabaseName("IDX_Projects_IsVisible");

            // Relationships
            builder.HasOne(p => p.Company)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
