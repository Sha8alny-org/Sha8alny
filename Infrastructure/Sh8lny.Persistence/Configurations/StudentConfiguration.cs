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
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            // Table mapping
            builder.ToTable("Students");

            // Primary key
            builder.HasKey(s => s.StudentID);

            // Properties
            builder.Property(s => s.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Phone)
                .HasMaxLength(20);

            builder.Property(s => s.ProfilePicture)
                .HasMaxLength(500);

            builder.Property(s => s.CvFileUrl)
                .HasMaxLength(500);

            builder.Property(s => s.StudentIDNumber)
                .HasMaxLength(50);

            builder.Property(s => s.City)
                .HasMaxLength(100);

            builder.Property(s => s.State)
                .HasMaxLength(100);

            builder.Property(s => s.Country)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("Egypt");

            builder.Property(s => s.ProfileCompleteness)
                .HasDefaultValue(0);

            builder.Property(s => s.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(StudentStatus.Active);

            builder.Property(s => s.AcademicYear)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(s => s.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            builder.Property(s => s.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Rating & Reviews
            builder.Property(s => s.AverageRating)
                .HasColumnType("decimal(3,2)")
                .HasDefaultValue(0);

            builder.Property(s => s.TotalReviews)
                .HasDefaultValue(0);

            builder.Property(s => s.TotalInternshipDays)
                .HasDefaultValue(0);

            // Ignore computed property
            builder.Ignore(s => s.FullName);

            // Indexes
            builder.HasIndex(s => s.UserID)
                .HasDatabaseName("IDX_Students_UserID");

            builder.HasIndex(s => s.UniversityID)
                .HasDatabaseName("IDX_Students_UniversityID");

            builder.HasIndex(s => s.DepartmentID)
                .HasDatabaseName("IDX_Students_DepartmentID");

            builder.HasIndex(s => s.Status)
                .HasDatabaseName("IDX_Students_Status");

            builder.HasIndex(s => s.AverageRating)
                .HasDatabaseName("IDX_Students_AverageRating");

            // Relationships
            builder.HasOne(s => s.University)
                .WithMany(u => u.Students)
                .HasForeignKey(s => s.UniversityID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(s => s.Department)
                .WithMany(d => d.Students)
                .HasForeignKey(s => s.DepartmentID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(s => s.StudentSkills)
                .WithOne(ss => ss.Student)
                .HasForeignKey(ss => ss.StudentID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Applications)
                .WithOne(a => a.Student)
                .HasForeignKey(a => a.StudentID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.GroupMemberships)
                .WithOne(gm => gm.Student)
                .HasForeignKey(gm => gm.StudentID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Certificates)
                .WithOne(c => c.Student)
                .HasForeignKey(c => c.StudentID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
